using artifact.shared.data;

namespace artifact.desktop.Messaging.Http
{
    public interface IArtifactHttpClient
    {
        Task<Artifact?> SubmitRecognitionAsync(ArtifactHttpContent content, CancellationToken cancellationToken = default);
    }
}
