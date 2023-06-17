using System;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

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
                var deviceTypes = esi.XPathSelectElements("//Descriptions/Devices/Device/Type").Select(e => e.Value);
                foreach (var deviceType in deviceTypes)
                {
                    _logger.LogDebug("Device: {deviceType}", deviceType);
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
