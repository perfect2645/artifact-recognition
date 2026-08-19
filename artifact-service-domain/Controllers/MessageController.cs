using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace artifact.service.domain.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [ApiVersion("0.1")]
    public class MessageController(ILogger<MessageController> logger) : ControllerBase
    {
        private readonly ILogger<MessageController> _logger = logger;

        [HttpPost]
        public IActionResult SendSignalrArtifact()
        {
            _logger.LogInformation("Received a POST request.");
            return Ok("Signalr artifact sent successfully!");
        }
    }
}
