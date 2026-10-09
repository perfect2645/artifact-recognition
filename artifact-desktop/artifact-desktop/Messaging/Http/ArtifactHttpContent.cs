using artifact.shared;
using artifact.shared.data;
using Messaging.Http.Content;
using Messaging.Http.Exceptions;
using System.Net.Http.Json;
using Utils.Generic;
using Utils.Json;

namespace artifact.desktop.Messaging.Http
{
    public class ArtifactHttpContent : HttpStringContent
    {
        private readonly RecognitionHttpRequest _artifactHttpRequest;

        public ArtifactHttpContent(RecognitionHttpRequest artifactHttpRequest)
        {
            if (artifactHttpRequest is null)
            {
                throw new HttpException("ArtifactHttpRequest cannot be null", HttpStatus.BuildRequestContent);
            }
            _artifactHttpRequest = artifactHttpRequest;

            AddContentHeader(SharedConstants.HttpTaskIdHeader, artifactHttpRequest.TaskId.NotNullString());
        }

        public override JsonContent GetJsonContent()
        {
            return JsonContent.Create(_artifactHttpRequest, options: JsonEncoder.JsonOption);
        }
    }
}
