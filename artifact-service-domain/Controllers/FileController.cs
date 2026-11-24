using artifact.service.domain.Filters.Files;
using artifact.service.domain.Models.Files;
using artifact.service.domain.Services.Files;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Utils.Json;

namespace artifact.service.domain.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ApiVersion(1.0)]
    public class FileController(IFileUploadService fileUploadService,
        ILogger<FileController> logger) : ControllerBase
    {
        [HttpGet]
        [TypeFilter<FileKeyValidationFilterAttribute>]
        public async Task<ActionResult<ApiResponse<UploadItem?>>> Get([FromQuery, Required] long fileSize,
            [FromQuery, Required] string sha256Hash)
        {
            var file = await fileUploadService.GetUploadItemAsync(fileSize, sha256Hash);
            var result = ApiResponse<UploadItem?>.Success(file);

            return Ok();
        }

        [HttpPost]
        [TypeFilter<FileUploadValidationFilterAttribute>]
        public async Task<ActionResult<UploadItem>> Upload(IFormFile file, [FromBody] string? description = null)
        {
            var uploadItem = await fileUploadService.UploadFileAsync(file, description);
            return Ok(uploadItem);
        }
    }
}
