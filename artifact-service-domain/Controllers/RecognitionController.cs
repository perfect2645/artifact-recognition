using artifact.service.domain.Services.Recognition;
using artifact.shared.data;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace artifact.service.domain.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ApiVersion("1.0")]
    public class RecognitionController(
        ILogger<RecognitionController> logger,
        IRecognitionService recognitionService) : ControllerBase
    {

        [HttpPost]
        public async Task<IActionResult> RecognizeAsync([FromBody] RecognitionHttpRequest request)
        {
            logger.LogInformation("Received recognition request for TaskId: {TaskId}, FolderPath: {FolderPath}, Artifacts count: {ArtifactsCount}",
                request.TaskId, request.FolderPath, request.Artifacts.Count);

            await recognitionService.SubmitRecognitionAsync(request, CancellationToken.None);

            return Ok();
        }
    }
}
