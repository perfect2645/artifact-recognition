using artifact.service.domain.Configurations;
using artifact.service.domain.Configurations.Files;
using artifact.service.domain.Models.Files;
using Microsoft.Extensions.Options;
using NetUtils.Aspnet.Generic;
using NetUtils.Repository;
using Utils.EncodingEx;
using Utils.Ioc;

namespace artifact.service.domain.Services.Files
{
    [Register]
    public class FileUploadService : IFileUploadService
    {
        private readonly IRepositorySlim<UploadItem, Guid> _repository;
        private readonly ILogger<FileUploadService> _logger;
        private readonly FileStorage _fileStorage;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly StaticFileOptions _staticFileOptions;

        public FileUploadService([FromKeyedServices(Constants.FileRepositoryIocKey)]IRepositorySlim<UploadItem, Guid> repository,
            ILogger<FileUploadService> logger,
            IOptions<FileStorage> fileStorageSettings,
            IHttpContextAccessor httpContextAccessor,
            StaticFileOptions staticFileOptions)
        {
            _repository = repository;
            _logger = logger;
            _fileStorage = fileStorageSettings.Value;
            _httpContextAccessor = httpContextAccessor;
            _staticFileOptions = staticFileOptions;
        }

        public async Task<UploadItem?> GetUploadItemAsync(long fileSize, string sha256Hash, CancellationToken cancellationToken = default)
        {
            return await _repository.GetAsync(item => item.FileSize == fileSize
                && item.FileHash == sha256Hash, cancellationToken);
        }

        public async ValueTask<UploadItem> UploadFileAsync(IFormFile formFile, string? description = null, CancellationToken cancellationToken = default)
        {
            var fileName = formFile.FileName;
            using var fileDataStream = formFile.OpenReadStream();
            return await UploadFileAsync(fileDataStream, fileName, description, cancellationToken);
        }

        public async ValueTask<UploadItem> UploadFileAsync(Stream fileDataStream, string fileName, string? description = null, CancellationToken cancellationToken = default)
        {
            _ = _httpContextAccessor.TryGetValue(Constants.Context_ExistingFile, out UploadItem? existingFile);
            if (existingFile is { } file)
            {
                _logger.LogDebug($"Uploading::File {existingFile.FileName}-{existingFile.Description} exists, skip uploading.");
                return existingFile;
            }

            var fileHashInContext = _httpContextAccessor.GetString(Constants.Context_FileHash);
            var fileHash = fileHashInContext ?? HashHelper.ComputeSHA256Hash(fileDataStream);
            long fileSize = fileDataStream.Length;
            var filePath = _fileStorage.RootPath;

            await SaveFileToStorageAsync(fileDataStream, filePath, cancellationToken);
            _logger.LogInformation($"Uploading::File {fileName}-{description} uploaded to {filePath}.");

            var uploadItem = new UploadItem(fileName, fileSize, fileHash, filePath, description);
            //TODO save file to repository.
            //await _repository.AddAsync(uploadItem, cancellationToken);

            return uploadItem;
        }

        private async Task SaveFileToStorageAsync(Stream fileDataStream, string filePath, CancellationToken cancellationToken)
        {
            var directoryPath = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(filePath);
            }

            try
            {
                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                await fileDataStream.CopyToAsync(fileStream, 81920, cancellationToken);
                // make sure all data is written to disk
                await fileStream.FlushAsync(cancellationToken);

            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning($"Uploading::File {filePath} upload cancelled during saving to storage.");
                throw;
            }
        }
    }
}
