using Messaging.Http.Response;

namespace artifact.shared.data
{
    public record RecognitionHttpResponse : ApiResult<IAsyncEnumerable<Artifact>>;
}
