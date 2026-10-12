using artifact.service.domain.Services.Signalr;
using artifact.shared.data;
using artifact.shared.logging;
using Utils.Ioc;

namespace artifact.service.domain.Services.Recognition
{
    [Register]
    public class RecognitionService(
        ILogger<RecognitionService> logger,
        IRealtimeService<ArtifactMessage> realtimeService) : IRecognitionService
    {
        public async Task SubmitRecognitionAsync(RecognitionHttpRequest request, CancellationToken cancellationToken)
        {
            logger.LogRecognitionProcessing(request.TaskId.ToString());

            foreach (var artifact in request.Artifacts)
            {
                artifact.RecognitionStatus = RecognitionStatus.Processing;
                var message = new ArtifactMessage(nameof(RecognitionService), nameof(Artifact), artifact);

                logger.LogArtifactStatus(request.TaskId.ToString(), nameof(SubmitRecognitionAsync), message);

                await realtimeService.SendGroupRealtimeAsync("recognition-model", "recognition", message, cancellationToken);
            }
        }
    }
}
