using Microsoft.AspNetCore.Mvc;
using Configurator.Models;
using static EtherCATInfoXml.Parser;

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
        /// Parse xs:complexType AccessType to EtherCATObjectAccess.
        /// </summary>
        /// <param name="access">AccessType to parse.</param>
        /// <returns>Parsed value.</returns>
        private static EtherCATObjectAccess? ParseAccess(EtherCATInfoXml.AccessType? access)
        {
            return access?.Value switch
            {
                null => null,
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
        private static EtherCATObjectPdoMapping? ParsePdoMapping(EtherCATInfoXml.ObjectTypeFlagsPdoMapping? pdoMapping)
        {
            return pdoMapping switch
            {
                null => null,
                EtherCATInfoXml.ObjectTypeFlagsPdoMapping.R or
                EtherCATInfoXml.ObjectTypeFlagsPdoMapping.R1 => EtherCATObjectPdoMapping.RxPDO,
                EtherCATInfoXml.ObjectTypeFlagsPdoMapping.T or
                EtherCATInfoXml.ObjectTypeFlagsPdoMapping.T1 => EtherCATObjectPdoMapping.TxPDO,
                EtherCATInfoXml.ObjectTypeFlagsPdoMapping.RT or
                EtherCATInfoXml.ObjectTypeFlagsPdoMapping.TR or
                EtherCATInfoXml.ObjectTypeFlagsPdoMapping.Rt or
                EtherCATInfoXml.ObjectTypeFlagsPdoMapping.Tr => EtherCATObjectPdoMapping.TxAndRxPDO,
                _ => throw new ArgumentException("Invalid enum value for pdoMapping", nameof(pdoMapping)),
            };
        }

        /// <summary>
        /// Parse xs:simpleType SubItemTypeFlagsPdoMapping to EtherCATObjectPdoMapping.
        /// </summary>
        /// <param name="access">SubItemTypeFlagsPdoMapping to parse.</param>
        /// <returns>Parsed value.</returns>
        private static EtherCATObjectPdoMapping? ParsePdoMapping(EtherCATInfoXml.SubItemTypeFlagsPdoMapping? pdoMapping)
        {
            return pdoMapping switch
            {
                null => null,
                EtherCATInfoXml.SubItemTypeFlagsPdoMapping.R or
                EtherCATInfoXml.SubItemTypeFlagsPdoMapping.R1 => EtherCATObjectPdoMapping.RxPDO,
                EtherCATInfoXml.SubItemTypeFlagsPdoMapping.T or
                EtherCATInfoXml.SubItemTypeFlagsPdoMapping.T1 => EtherCATObjectPdoMapping.TxPDO,
                EtherCATInfoXml.SubItemTypeFlagsPdoMapping.RT or
                EtherCATInfoXml.SubItemTypeFlagsPdoMapping.TR or
                EtherCATInfoXml.SubItemTypeFlagsPdoMapping.Rt or
                EtherCATInfoXml.SubItemTypeFlagsPdoMapping.Tr => EtherCATObjectPdoMapping.TxAndRxPDO,
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
                // Deserialize an pre-process ESI
                var esi = Parse(file.OpenReadStream());

                // Parse devices
                var devices = esi?.Descriptions.Devices.Select(d => new EtherCATDevice
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
                            Access = ParseAccess(o.Flags?.Access),
                            PdoMapping = ParsePdoMapping(o.Flags?.PdoMapping),
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
                            }) ?? Enumerable.Empty<EtherCATObject>(),
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

                if (devices == null || devices.Count() <= 0)
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
            catch (System.Xml.XmlException ex)
            {
                return BadRequest($"XML syntax error: {ex.Message}");
            }
            catch (System.Xml.Schema.XmlSchemaValidationException ex)
            {
                return BadRequest($"XML schema error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
