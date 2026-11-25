using artifact.service.domain.Services.Files;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Utils.Enumerable;

namespace artifact.service.domain.Filters.Files
{
    public class FileKeyValidationFilterAttribute(IFileUploadService fileUploadService): ActionFilterAttribute
    {
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // TODO check if we could use something like NetUtils.Aspnet.Errors.ApiProblemDetailsFactory

            long? fileSize = context.ActionArguments.TryGetValueAs("fileSize", out long size) ? size : null;

            if (!ValidateFileSize(context, fileSize))
            {
                return;
            }

            await next();

        }
        private bool ValidateFileSize(ActionExecutingContext context, long? fileSize)
        {
            // 50mb limit
            if (fileSize is >= 0 and <= 50_000_000)
            {
                return true;
            }

            context.ModelState.AddModelError("fileSize", "The file size must be between 0 and 50 MB.");
            var problemDetails = new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid file size",
                Detail = "The provided file size is invalid."
            };

            context.Result = new BadRequestObjectResult(problemDetails);
            return false;
        }
    }

}
