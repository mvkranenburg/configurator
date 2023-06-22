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
        private static string ParseNameType(IEnumerable<EtherCATInfoXmlSchema.NameType> nameTypes, string lcId = "1033")
        {
            return nameTypes.Where(n => n.LcId == lcId).FirstOrDefault()?.Value;
        }

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
                            Name = ParseNameType(o.Name),
                            Comment = ParseNameType(o.Comment),
                        }) ?? Enumerable.Empty<EtherCATObject>(),

                        // Add RxPDO objects
                        d.RxPdo.SelectMany(r => r.Entry.Where(e => e.DataType != null).Select(e => new EtherCATObject
                        {
                            Index = (ushort)ParseHexDecValue(e.Index.Value),
                            Type = e.DataType.Value,
                            Name = ParseNameType(e.Name),
                            Comment = e.Comment,
                        })),

                        // Add TxPDO objects
                        d.TxPdo.SelectMany(r => r.Entry.Where(e => e.DataType != null).Select(e => new EtherCATObject
                        {
                            Index = (ushort)ParseHexDecValue(e.Index.Value),
                            Type = e.DataType.Value,
                            Name = ParseNameType(e.Name),
                            Comment = e.Comment,
                        }))
                    }.SelectMany(o => o),
                });

                var numDevices = devices.Count();
                if (numDevices <= 0)
                    throw new XmlContentException("File contains no EtherCAT devices.");

                return Ok(new UploadEsiResponse { Name = file.Name, Size = file.Length, Devices = devices });
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
