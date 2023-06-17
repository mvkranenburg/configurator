using System;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Configurator.Models;

namespace Configurator.Server.Controllers
{
    public partial class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger _logger;

        public UploadController(IWebHostEnvironment environment, ILogger<UploadController> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        // Single file upload
        [HttpPost("upload/esi")]
        public IActionResult EtherCATSlaveInformation(IFormFile file)
        {
            try
            {
                // TODO: Validate IFormFile input against XSD
                var esi = XDocument.Load(file.OpenReadStream());
                var devices = esi.XPathSelectElements("//Descriptions/Devices/Device")
                                     .Select(e => new Device
                                     {
                                         Type = e.Element("Type").Value,
                                         Name = e.Elements("Name").Where(n => (string)n.Attribute("LcId") == "1033").FirstOrDefault().Value,
                                         ProductCode = uint.Parse(((string)e.Element("Type").Attribute("ProductCode")).Substring(2), NumberStyles.HexNumber),
                                         RevisionNo = uint.Parse(((string)e.Element("Type").Attribute("RevisionNo")).Substring(2), NumberStyles.HexNumber)
                                     });
                foreach (var device in devices)
                {
                    _logger.LogDebug(device.ToString());
                }

                // Put your code here
                return StatusCode(200);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
