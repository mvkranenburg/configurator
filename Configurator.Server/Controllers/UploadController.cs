using System.Globalization;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Mvc;
using Configurator.Models;

namespace Configurator.Server.Controllers
{
    public partial class UploadController : ControllerBase
    {
        public class XmlContentException : Exception
        {
            public XmlContentException() { }
            public XmlContentException(string message) : base(message) { }
            public XmlContentException(string message, Exception inner) : base(message, inner) { }
        }

        private readonly IWebHostEnvironment _environment;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of UploadController.
        /// </summary>
        /// <param name="environment">Web-host environment.</param>
        /// <param name="logger">Logger instance.</param>
        public UploadController(IWebHostEnvironment environment, ILogger<UploadController> logger) =>
            (_environment, _logger) = (environment, logger);

        /// <summary>
        /// Parse xs:simpleType HexDecValue to long.
        /// </summary>
        /// <param name="hexDecValue">HexDecValue to parse.</param>
        /// <returns>Parsed value.</returns>
        private static long ParseHexDecValue(string hexDecValue)
        {
            if (hexDecValue.StartsWith("#x"))
                return long.Parse(hexDecValue[2..], NumberStyles.HexNumber);
            else
                return long.Parse(hexDecValue);
        }

        /// <summary>
        /// Parse xs:complexType NameType to string.
        /// </summary>
        /// <param name="nameTypes">Collection of NameType to parse.</param>
        /// <param name="lcid">Language ID to parse, default is 1033 (English)</param>
        /// <returns>Parsed value.</returns>
        private static string ParseNameType(IEnumerable<EtherCATInfoXmlSchema.NameType> nameTypes, int lcId = 1033)
        {
            return nameTypes.Where(n => n.LcId == lcId).FirstOrDefault()?.Value;
        }

        /// <summary>
        /// Parse xs:complexType AccessType to EtherCATObjectAccess.
        /// </summary>
        /// <param name="access">AccessType to parse.</param>
        /// <returns>Parsed value.</returns>
        private static EtherCATObjectAccess ParseAccess(EtherCATInfoXmlSchema.AccessType access)
        {
            return access.Value switch
            {
                "ro" => EtherCATObjectAccess.ReadOnly,
                "rw" => EtherCATObjectAccess.ReadWrite,
                "wo" => EtherCATObjectAccess.WriteOnly,
                _ => throw new ArgumentException("Invalid sting value for access", nameof(access)),
            };
        }

        /// <summary>
        /// Parse xs:simpleType ObjectTypeFlagsPdoMapping to EtherCATObjectPdoMapping.
        /// </summary>
        /// <param name="access">ObjectTypeFlagsPdoMapping to parse.</param>
        /// <returns>Parsed value.</returns>
        private static EtherCATObjectPdoMapping ParsePdoMapping(EtherCATInfoXmlSchema.ObjectTypeFlagsPdoMapping pdoMapping)
        {
            return pdoMapping switch
            {
                EtherCATInfoXmlSchema.ObjectTypeFlagsPdoMapping.R or
                EtherCATInfoXmlSchema.ObjectTypeFlagsPdoMapping.R1 => EtherCATObjectPdoMapping.RxPDO,
                EtherCATInfoXmlSchema.ObjectTypeFlagsPdoMapping.T or
                EtherCATInfoXmlSchema.ObjectTypeFlagsPdoMapping.T1 => EtherCATObjectPdoMapping.TxPDO,
                EtherCATInfoXmlSchema.ObjectTypeFlagsPdoMapping.RT or
                EtherCATInfoXmlSchema.ObjectTypeFlagsPdoMapping.TR or
                EtherCATInfoXmlSchema.ObjectTypeFlagsPdoMapping.Rt or
                EtherCATInfoXmlSchema.ObjectTypeFlagsPdoMapping.Tr => EtherCATObjectPdoMapping.TxAndRxPDO,
                _ => throw new ArgumentException("Invalid enum value for pdoMapping", nameof(pdoMapping)),
            };
        }

        /// <summary>
        /// Parse xs:simpleType SubItemTypeFlagsPdoMapping to EtherCATObjectPdoMapping.
        /// </summary>
        /// <param name="access">SubItemTypeFlagsPdoMapping to parse.</param>
        /// <returns>Parsed value.</returns>
        private static EtherCATObjectPdoMapping ParsePdoMapping(EtherCATInfoXmlSchema.SubItemTypeFlagsPdoMapping pdoMapping)
        {
            return pdoMapping switch
            {
                EtherCATInfoXmlSchema.SubItemTypeFlagsPdoMapping.R or
                EtherCATInfoXmlSchema.SubItemTypeFlagsPdoMapping.R1 => EtherCATObjectPdoMapping.RxPDO,
                EtherCATInfoXmlSchema.SubItemTypeFlagsPdoMapping.T or
                EtherCATInfoXmlSchema.SubItemTypeFlagsPdoMapping.T1 => EtherCATObjectPdoMapping.TxPDO,
                EtherCATInfoXmlSchema.SubItemTypeFlagsPdoMapping.RT or
                EtherCATInfoXmlSchema.SubItemTypeFlagsPdoMapping.TR or
                EtherCATInfoXmlSchema.SubItemTypeFlagsPdoMapping.Rt or
                EtherCATInfoXmlSchema.SubItemTypeFlagsPdoMapping.Tr => EtherCATObjectPdoMapping.TxAndRxPDO,
                _ => throw new ArgumentException("Invalid enum value for pdoMapping", nameof(pdoMapping)),
            };
        }

        /// <summary>
        /// Deserialize and pre-process EtherCAT Slave Information (ESI) file. The deserialized ESI is pre-processed before return
        /// for easier parsing. The pre-processing steps are:
        /// - Expand SubItems referring to an ArrayInfo DataType
        ///
        /// Constraints:
        /// - Only Devices with a single Profile are supported, exception is thrown otherwise
        /// - Only DataTypes with a single ArrayInfo are supported, exception is thrown otherwise
        /// </summary>
        /// <param name="stream">Stream containing the ESI file.</param>
        /// <returns><c>EtherCATInfoXmlSchema.EtherCATInfo</c> representing the ESI file.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown when XML parsing fails.</exception>
        private static EtherCATInfoXmlSchema.EtherCATInfo DeserializeEtherCATInfo(System.IO.Stream stream)
        {
            // TODO: Validate input against XSD

            // Deserialize XML
            var serializer = new XmlSerializer(typeof(EtherCATInfoXmlSchema.EtherCATInfo));
            var esi = (EtherCATInfoXmlSchema.EtherCATInfo)serializer.Deserialize(stream);

            // Expand SubItems referring to an ArrayInfo DataType
            for (var i = 0; i < esi.Descriptions.Devices.Count; i++)
            {
                // Only continue if a Profile is defined
                var profile = esi.Descriptions.Devices[i].Profile;
                if (profile.Count == 0)
                    continue;

                var dataTypes = profile.Single().Dictionary.DataTypes;
                var arrayInfos = dataTypes.Where(d => d.ArrayInfo.Count() > 0)
                    .Select(d => (d.Name, d.BaseType, d.BitSize, d.ArrayInfo.Single().LBound, d.ArrayInfo.Single().Elements))
                    .ToDictionary(d => d.Name);

                for (var j = 0; j < dataTypes.Count; j++)
                {
                    var subItems = dataTypes[j].SubItem;

                    // CoE data type ARRAY conditions (ETG.2000 v1.0.14 table 10):
                    // - must contain exactly two SubItems
                    // - 1st element must have SubIdx 0
                    // - 1st element must have Type USINT
                    // - 2nd element must have no SubIdx
                    // - 2nd element must have ArrayInfo datatype
                    if
                    (
                        subItems.Count == 2 &&
                        ParseHexDecValue(subItems[0].SubIdx) == 0 &&
                        subItems[0].Type == "USINT" &&
                        subItems[1].SubIdx == null &&
                        arrayInfos.ContainsKey(subItems[1].Type)
                    )
                    {
                        var bitOffs = subItems[1].BitOffs;
                        var flags = subItems[1].Flags;
                        var arrayInfo = arrayInfos[subItems[1].Type];

                        subItems.RemoveAt(1);

                        for (var k = arrayInfo.LBound; k < arrayInfo.LBound + arrayInfo.Elements; k++)
                        {
                            var subItem = new EtherCATInfoXmlSchema.SubItemType
                            {
                                SubIdx = $"{k}",
                                Name = $"Element[{k - 1}]",
                                Type = arrayInfo.BaseType,
                                BitSize = arrayInfo.BitSize,
                                BitOffs = bitOffs,
                                Flags = flags,
                            };
                            subItems.Add(subItem);

                            bitOffs += arrayInfo.BitSize;
                        }
                    }
                }
            }

            return esi;
        }

        /// <summary>
        /// Upload handler for EtherCAT Slave Information (ESI) file on the path <c>upload/esi</c>. The 
        /// ESI file is converted to an <c>IEnumerable&lt;EtherCATDevice&gt;</c> collection of devices
        /// and returned to the caller on success.
        /// </summary>
        /// <param name="file">File sent with the HTTP request.</param>
        /// <returns>HTTP response with status code and in case of status code 200 a JSON serialized <c>UploadEsiResponse</c>.</returns>
        [HttpPost("upload/esi")]
        public IActionResult EtherCATSlaveInformation(IFormFile file)
        {
            try
            {
                // Deserialize an pre-process ESI
                var esi = DeserializeEtherCATInfo(file.OpenReadStream());

                // Parse devices
                var devices = esi.Descriptions.Devices.Select(d => new EtherCATDevice
                {
                    Type = d.Type.Value,
                    Name = ParseNameType(d.Name),
                    ProductCode = (uint)ParseHexDecValue(d.Type.ProductCode),
                    RevisionNo = (uint)ParseHexDecValue(d.Type.RevisionNo),
                    Objects = new[]
                    {
                        // Add Dictionary objects
                        d.Profile.SingleOrDefault()?.Dictionary.Objects.Select(o => new EtherCATObject
                        {
                            Index = (ushort)ParseHexDecValue(o.Index.Value),
                            Type = o.Type,
                            BitSize = o.BitSize,
                            Access = ParseAccess(o.Flags.Access),
                            PdoMapping = ParsePdoMapping(o.Flags.PdoMapping),
                            Name = ParseNameType(o.Name),
                            Comment = ParseNameType(o.Comment),
                            Objects = d.Profile.SingleOrDefault()?.Dictionary.DataTypes.Where(d => d.Name == o.Type).SingleOrDefault()?.SubItem.Select(s => new EtherCATObject
                            {
                                Index = (ushort)ParseHexDecValue(o.Index.Value),
                                SubIndex = (byte)ParseHexDecValue(s.SubIdx),
                                Type = s.Type,
                                Access = ParseAccess(s.Flags.Access),
                                PdoMapping = ParsePdoMapping(s.Flags.PdoMapping),
                                Name = s.Name,
                                Comment = ParseNameType(s.Comment),
                                Source = new EtherCATObjectSource
                                {
                                    Type = EtherCATObjectSourceType.Dictionary
                                },
                            }),
                            Source = new EtherCATObjectSource
                            {
                                Type = EtherCATObjectSourceType.Dictionary
                            },
                        }) ?? Enumerable.Empty<EtherCATObject>(),

                        // Add RxPDO objects
                        d.RxPdo.SelectMany(r => r.Entry.Where(e => e.DataType != null).Select(e => new EtherCATObject
                        {
                            Index = (ushort)ParseHexDecValue(e.Index.Value),
                            SubIndex = (byte)ParseHexDecValue(e.SubIndex),
                            Type = e.DataType.Value,
                            BitSize = e.BitLen,
                            Access = EtherCATObjectAccess.WriteOnly,
                            PdoMapping = EtherCATObjectPdoMapping.RxPDO,
                            Name = ParseNameType(e.Name),
                            Comment = e.Comment,
                            Source = new EtherCATObjectSource
                            {
                                Type = EtherCATObjectSourceType.RxPDO,
                                Index = (ushort)ParseHexDecValue(r.Index.Value),
                                Name = ParseNameType(r.Name),
                            },
                        })),

                        // Add TxPDO objects
                        d.TxPdo.SelectMany(r => r.Entry.Where(e => e.DataType != null).Select(e => new EtherCATObject
                        {
                            Index = (ushort)ParseHexDecValue(e.Index.Value),
                            SubIndex = (byte)ParseHexDecValue(e.SubIndex),
                            Type = e.DataType.Value,
                            BitSize = e.BitLen,
                            Access = EtherCATObjectAccess.ReadOnly,
                            PdoMapping = EtherCATObjectPdoMapping.TxPDO,
                            Name = ParseNameType(e.Name),
                            Comment = e.Comment,
                            Source = new EtherCATObjectSource
                            {
                                Type = EtherCATObjectSourceType.TxPDO,
                                Index = (ushort)ParseHexDecValue(r.Index.Value),
                                Name = ParseNameType(r.Name),
                            },
                        }))
                    }.SelectMany(o => o),
                });

                var numDevices = devices.Count();
                if (numDevices <= 0)
                    throw new XmlContentException("File contains no EtherCAT devices.");

                return Ok(new UploadEsiResponse
                {
                    Name = file.Name,
                    Size = file.Length,
                    Devices = devices
                });
            }
            catch (XmlContentException ex)
            {
                return BadRequest($"XML content error: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest($"XML syntax error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
