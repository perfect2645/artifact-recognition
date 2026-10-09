using artifact.shared.data;

namespace artifact.desktop.Services
{
    public interface IRecognitionService
    {
        ValueTask<RecognitionHttpResponse> SubmitRecognitionAsync(RecognitionHttpRequest request, CancellationToken cancellationToken);
    }
}
