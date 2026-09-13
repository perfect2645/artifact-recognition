using Messaging.Http.Response;

namespace artifact.shared.data
{
    public record ArtifactHttpResponse : ApiResult<IAsyncEnumerable<Artifact>>;
}
