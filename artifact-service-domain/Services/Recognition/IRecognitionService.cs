using artifact.shared.data;

namespace artifact.service.domain.Services.Recognition
{
    public interface IRecognitionService
    {
        Task SubmitRecognitionAsync(RecognitionHttpRequest request, CancellationToken cancellationToken);
    }
}
