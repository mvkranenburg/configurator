using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Globalization;
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

        [HttpPost("upload/esi")]
        public IActionResult EtherCATSlaveInformation(IFormFile file)
        {
            try
            {
                // TODO: Validate IFormFile input against XSD

                var esi = XDocument.Load(file.OpenReadStream());
                var devices = esi.XPathSelectElements("//Descriptions/Devices/Device").Select(e => new Device
                {
                    Type = e.Element("Type").Value,
                    Name = e.Elements("Name").Where(n => (string)n.Attribute("LcId") == "1033").FirstOrDefault().Value,
                    ProductCode = uint.Parse(((string)e.Element("Type").Attribute("ProductCode"))[2..], NumberStyles.HexNumber),
                    RevisionNo = uint.Parse(((string)e.Element("Type").Attribute("RevisionNo"))[2..], NumberStyles.HexNumber)
                });

                foreach (var device in devices)
                {
                    _logger.LogDebug("{device}", device.ToString());
                }

                var numDevices = devices.Count();
                if (numDevices <= 0)
                    throw new XmlContentException("File contains no EtherCAT devices.");

                return Ok(new { Name = file.FileName, Size = file.Length, NumDevices = numDevices });
            }
            catch (XmlContentException ex)
            {
                return BadRequest($"XML content error: {ex.Message}");
            }
            catch (XmlException ex)
            {
                return BadRequest($"XML syntax error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"{ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
