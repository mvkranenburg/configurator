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
        /// Parse xs:complexType ObjectTypeFlagsAccess to EtherCATObjectAccess.
        /// </summary>
        /// <param name="access">ObjectTypeFlagsAccess to parse.</param>
        /// <returns>Parsed value.</returns>
        private static EtherCATObjectAccess ParseAccess(EtherCATInfoXmlSchema.ObjectTypeFlagsAccess access)
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
                // TODO: Validate IFormFile input against XSD

                // Deserialize XML
                var serializer = new XmlSerializer(typeof(EtherCATInfoXmlSchema.EtherCATInfo));
                var esi = (EtherCATInfoXmlSchema.EtherCATInfo)serializer.Deserialize(file.OpenReadStream());

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
                        d.Profile.FirstOrDefault()?.Dictionary.Objects.Select(o => new EtherCATObject
                        {
                            Index = (ushort)ParseHexDecValue(o.Index.Value),
                            Type = o.Type,
                            BitSize = o.BitSize,
                            Access = ParseAccess(o.Flags.Access),
                            PdoMapping = ParsePdoMapping(o.Flags.PdoMapping),
                            Name = ParseNameType(o.Name),
                            Comment = ParseNameType(o.Comment),
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
