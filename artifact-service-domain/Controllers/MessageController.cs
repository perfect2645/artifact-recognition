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

        [HttpPost]
        [Route("signalr-artifact/{group}/{eventName}")]
        public async Task<IActionResult> SendSignalrArtifact(
            string group, 
            string? eventName,
            [FromBody] ArtifactMessage message)
        {
            await realtimeService.SendGroupRealtimeAsync(group, eventName, message);
            logger.LogInformation("Received a POST request.");
            return Ok("Signalr artifact sent successfully!");
        }
    }
}
