using artifact.desktop.Generic;
using artifact.desktop.Messaging.Http;
using artifact.shared.data;
using artifact.shared.logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Utils.Ioc;

namespace artifact.desktop.Services
{
    [Register(ServiceType = typeof(IRecognitionService), Lifetime = Lifetime.Singleton)]
    public class RecognitionService(
        [FromKeyedServices(Constants.ArtifactHttpApiKey)]IArtifactHttpClient artifactHttpClient, 
        ILogger<RecognitionService> logger) : IRecognitionService
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
