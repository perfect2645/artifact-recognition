using artifact.desktop.Generic;
using artifact.desktop.Messaging.Http;
using artifact.shared.data;
using Messaging.Http.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using Utils.Ioc;

namespace artifact.desktop.Services
{
    [Register(ServiceType = typeof(IRecognitionService), Lifetime = Lifetime.Singleton)]
    public class RecognitionService(
        [FromKeyedServices(Constants.ArtifactHttpApiKey)]IArtifactHttpClient artifactHttpClient) : IRecognitionService
    {
        public async ValueTask<RecognitionHttpResponse> SubmitRecognitionAsync(RecognitionHttpRequest request, CancellationToken cancellationToken)
        {
            var content = new ArtifactHttpContent(request);

            try
            {
                var result = await artifactHttpClient.SubmitRecognitionAsync(content, cancellationToken);
            }
            catch (HttpException ex)
            {
                return new RecognitionHttpResponse();
            }
        }
    }
}
