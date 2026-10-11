using artifact.shared.data;

namespace artifact.desktop.Messaging.Http
{
    public interface IArtifactHttpClient
    {
        Task<RecognitionHttpResponse?> SubmitRecognitionAsync(ArtifactHttpContent content, CancellationToken cancellationToken = default);
    }
}
