using artifact.shared.data;
using Messaging.Http.Client;
using Messaging.Http.Exceptions;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace artifact.desktop.Messaging.Http
{
    public class ArtifactHttpClient(
        HttpClient httpClient,
        ILogger<ArtifactHttpClient> logger) : HttpApiClient(httpClient)
    {
        public async Task<Artifact?> CreateArtifact(ArtifactHttpContent content)
        {
            try
            {
                return await PostAsync<Artifact>(content);
            }
            catch (HttpException ex)
            {
                logger.LogError(ex, "HttpException occurred while creating artifact.");
                return null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error occurred while creating artifact.");
                return null;
            }
        }
    }
}
