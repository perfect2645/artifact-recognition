using artifact.shared.data;

namespace artifact.desktop.Messaging.Http
{
    public interface IArtifactHttpClient
    {
        Task<Artifact?> CreateArtifact(ArtifactHttpContent content, CancellationToken cancellationToken = default);
    }
}
