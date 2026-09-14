using artifact.shared.data;

namespace artifact.desktop.Services
{
    public interface IRecognitionService
    {
        Task<ArtifactHttpResponse> CreateArtifactsAsync(string inputFolderPath, CancellationToken cancellationToken);
    }
}
