using artifact.service.domain.Models.Files;

namespace artifact.service.domain.Repositories.Files
{
    public interface IFileRepository
    {
        ValueTask<UploadItem?> FindFileAsync(long fileSize, string sha256Hash, CancellationToken cancellationToken = default);
    }
}
