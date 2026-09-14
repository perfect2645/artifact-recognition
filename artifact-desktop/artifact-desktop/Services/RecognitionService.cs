using artifact.desktop.Messaging.Http;
using artifact.shared.data;
using artifact.shared.logging;
using Microsoft.Extensions.Logging;
using Utils.Ioc;

namespace artifact.desktop.Services
{
    [Register(ServiceType = typeof(RecognitionService), Lifetime = Lifetime.Singleton)]
    public class RecognitionService(IArtifactHttpClient artifactHttpClient, ILogger<RecognitionService> logger) : IRecognitionService
    {
        public async Task<ArtifactHttpResponse> CreateArtifactsAsync(string inputFolderPath, CancellationToken cancellationToken)
        {
            var taskId = Guid.NewGuid().ToString();
            logger.LogRecognitionProcessing(taskId);

            await artifactHttpClient.CreateArtifactAsync(new ArtifactHttpContent(), cancellationToken);
            return new ArtifactHttpResponse();
        }
    }
}
