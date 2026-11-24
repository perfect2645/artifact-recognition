using NetUtils.Aspnet.Repository;
using System.ComponentModel.DataAnnotations;

namespace artifact.service.domain.Models.Files
{
    public record UploadItem(
        [property: MaxLength(128)] string FileName,
        long FileSize,
        string FileHash,
        string FilePath,
        string? Description
    ) : IEntity, IEntityTiming
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public DateTime CreateTime { get; init; }
        public DateTime? UpdateTime { get; set; }
    }
}
