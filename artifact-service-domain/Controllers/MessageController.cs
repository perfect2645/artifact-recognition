using artifact.service.domain.Services.Signalr;
using artifact.shared.data;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace artifact.service.domain.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [ApiVersion("0.1")]
    public class MessageController(
        ILogger<MessageController> logger,
        IRealtimeService<ArtifactMessage> realtimeService) : ControllerBase
    {
        private readonly ILogger<MessageController> _logger = logger;
        private readonly IRealtimeService<ArtifactMessage> _realtimeService = realtimeService;

        [HttpPost]
        [Route("signalr-artifact")]
        public async Task<IActionResult> SendSignalrArtifact([FromBody] ArtifactMessage message)
        {
            await _realtimeService.SendRealtimeAsync(message);
            _logger.LogInformation("Received a POST request.");
            return Ok("Signalr artifact sent successfully!");
        }
    }
}
