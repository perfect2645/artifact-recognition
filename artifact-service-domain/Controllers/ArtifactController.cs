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
    public class ArtifactController(IRealtimeService<ArtifactMessage> realtimeService) : ControllerBase
    {

        /// <summary>
        /// Updates an artifact and sends a real-time message to connected clients.
        /// </summary>
        /// <param name="artifactMessage"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] ArtifactMessage artifactMessage)
        {
                // TODO  persist the result before notifying clients
                // Let the global exception filter produce the common error response on failure.

                await realtimeService.SendGroupRealtimeAsync(
                    SharedConstants.SignalrClientGroup, 
                    SharedConstants.SignalrClientReceiveMessage, 
                    artifactMessage);

                return Ok(new { message = "Artifact updated successfully." });
        }
    }
}
