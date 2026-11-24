using artifact.service.domain.Models.Files;

namespace artifact.service.domain.Services.Files
{
    public interface IFileUploadService
    {
        Task<UploadItem?> GetUploadItemAsync(long fileSize, string sha256Hash, CancellationToken cancellationToken = default);
        ValueTask<UploadItem> UploadFileAsync(Stream fileStream, string fileName, string? description = null, CancellationToken cancellationToken = default);
        ValueTask<UploadItem> UploadFileAsync(IFormFile formFile, string? description = null, CancellationToken cancellationToken = default);
    }
}
