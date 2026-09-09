using Messaging.Http.Client;
using System.Net.Http;
using Utils.Ioc;

namespace artifact.desktop.Messaging.Http
{
    [Register(ServiceType = typeof(HttpApiClient), Lifetime = Lifetime.Singleton)]
    public class ArtifactHttpClient : HttpApiClient
    {
        public ArtifactHttpClient(HttpClient httpClient) : base(httpClient)
        {
        }
    }
}
