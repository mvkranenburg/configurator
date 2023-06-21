using System.Xml.Serialization;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Configurator.Models;
using EtherCATInfoXmlSchema;

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
        /// Parse a string of XML Schema simple type HexDecValue to a long.
        /// </summary>
        /// <param name="value">String to parse.</param>
        /// <returns>Long value of parsed string.</returns>
        public static long ParseHexDecValue(string value)
        {
            if (value.StartsWith("#x"))
                return long.Parse(value[2..], NumberStyles.HexNumber);
            else
                return long.Parse(value);
        }

        [HttpPost("upload/esi")]
        public IActionResult EtherCATSlaveInformation(IFormFile file)
        {
            try
            {
                // TODO: Validate IFormFile input against XSD

                var serializer = new XmlSerializer(typeof(EtherCATInfo));
                var esi = (EtherCATInfo)serializer.Deserialize(file.OpenReadStream());
                var devices = esi.Descriptions.Devices.Select(d => new Device
                {
                    Type = d.Type.Value,
                    Name = d.Name.Where(n => n.LcId == "1033").FirstOrDefault().Value,
                    ProductCode = (uint)ParseHexDecValue(d.Type.ProductCode),
                    RevisionNo = (uint)ParseHexDecValue(d.Type.RevisionNo),
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
