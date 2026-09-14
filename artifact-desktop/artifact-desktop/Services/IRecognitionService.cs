using artifact.shared.data;

namespace artifact.desktop.Services
{
    public interface IRecognitionService
    {
        ValueTask<ArtifactHttpResponse> CreateArtifactsAsync(string inputFolderPath, CancellationToken cancellationToken);
    }
}
