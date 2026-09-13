using artifact.desktop.Messaging.Http;
using artifact.shared.data;
using Utils.Ioc;

namespace artifact.desktop.Services
{
    [Register(ServiceType = typeof(RecognitionService), Lifetime = Lifetime.Singleton)]
    public class RecognitionService(IArtifactHttpClient artifactHttpClient) : IRecognitionService
    {
        public async Task<ArtifactHttpResponse> CreateArtifacts(string inputFolderPath, CancellationToken cancellationToken)
        {
            await artifactHttpClient.CreateArtifact(new ArtifactHttpContent(), cancellationToken);
            return new ArtifactHttpResponse();
        }
    }
}
