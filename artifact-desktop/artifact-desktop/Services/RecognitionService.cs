using artifact.desktop.Generic;
using artifact.desktop.Messaging.Http;
using artifact.shared.data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
using Utils.Ioc;

namespace artifact.desktop.Services
{
    [Register(ServiceType = typeof(IRecognitionService), Lifetime = Lifetime.Singleton)]
    public class RecognitionService(
        [FromKeyedServices(Constants.ArtifactHttpApiKey)]IArtifactHttpClient artifactHttpClient) : IRecognitionService
    {
        public async ValueTask<ArtifactHttpResponse> CreateArtifactsAsync(string inputFolderPath, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(inputFolderPath))
            {
                throw new ArgumentException("Input Folder Path is empty.");
            }

            if (Directory.Exists(inputFolderPath))
            {
                throw new ArgumentException($"Invalid input folder path {inputFolderPath}.");
            }

            await artifactHttpClient.CreateArtifactAsync(new ArtifactHttpContent(), cancellationToken);
            return new ArtifactHttpResponse();
        }
    }
}
