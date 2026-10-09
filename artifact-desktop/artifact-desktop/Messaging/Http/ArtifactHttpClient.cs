using artifact.shared.data;
using Messaging.Http.Client;
using Messaging.Http.Exceptions;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace artifact.desktop.Messaging.Http
{
    public class ArtifactHttpClient(
        HttpClient httpClient,
        ILogger<ArtifactHttpClient> logger) : HttpApiClient(httpClient), IArtifactHttpClient
    {
        public async Task<Artifact?> SubmitRecognitionAsync(ArtifactHttpContent content, CancellationToken cancellationToken = default)
        {
            try
            {
                return await PostAsync<Artifact>(content, cancellationToken);
            }
            catch (HttpException ex)
            {
                logger.LogError(ex, "HttpException occurred while creating artifact.");
                throw;
            }
            catch (Exception ex)
            {
                var errMsg = "Unexpected error occurred while creating artifact.";
                logger.LogError(ex, errMsg);
                throw new HttpException(errMsg, HttpStatus.Exception);
            }
        }
    }
}
