using artifact.service.domain.Services.Signalr;
using artifact.shared;
using artifact.shared.data;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace artifact.service.domain.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ApiVersion(0.1)]
    public class ArtifactController(ILogger<ArtifactController> logger,
        IRealtimeService<ArtifactMessage> realtimeService) : ControllerBase
    {

        /// <summary>
        /// Updates an artifact and sends a real-time message to connected clients.
        /// </summary>
        /// <param name="artifactMessage"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] ArtifactMessage artifactMessage)
        {
            try
            {
                // TODO  save it to a database or perform some business logic
                // Return a success response

                await realtimeService.SendGroupRealtimeAsync(
                    SharedConstants.SignalrClientGroup, 
                    SharedConstants.SignalrClientReceiveMessage, 
                    artifactMessage);

                return Ok(new { message = "Artifact updated successfully." });

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update artifact {@artifactMessage}.", artifactMessage);
                return StatusCode(500, new { error = "Failed to update artifact.", details = ex.Message });
            }
        }
    }
}
