using artifact.service.domain.Configurations;
using artifact.service.domain.Models.Files;
using NetUtils.Repository;
using System.Linq.Expressions;
using Utils.Ioc;

namespace artifact.service.domain.Repositories.Files
{
    [Register(Key = Constants.FileRepositoryIocKey, ServiceType = typeof(IRepositorySlim<UploadItem, Guid>))]
    public class FileRepository : IRepositorySlim<UploadItem, Guid>, IFileRepository
    {
        public async ValueTask<UploadItem?> FindFileAsync(long fileSize, string sha256Hash, CancellationToken cancellationToken = default)
        {
            return await Task.FromResult<UploadItem?>(null);
        }

        #region override

        public ValueTask<UploadItem> AddAsync(UploadItem entity, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask<UploadItem?> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ExistAsync(Expression<Func<UploadItem, bool>> predicate, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<UploadItem>> GetAllAsync(Expression<Func<UploadItem, bool>>? predicate = null, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<UploadItem?> GetAsync(Expression<Func<UploadItem, bool>> predicate, bool asNoTracking = true, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask<UploadItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask UpdateAsync(UploadItem entity)
        {
            throw new NotImplementedException();
        }

        #endregion override
    }
}
