using artifact.shared.data;

namespace artifact.desktop.Messaging.Http
{
    public interface IArtifactHttpClient
    {
        Task<Artifact?> CreateArtifactAsync(ArtifactHttpContent content, CancellationToken cancellationToken = default);
    }
}
