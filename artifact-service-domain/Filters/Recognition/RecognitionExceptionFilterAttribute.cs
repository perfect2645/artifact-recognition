using Microsoft.AspNetCore.Mvc.Filters;

namespace artifact.service.domain.Filters.Recognition
{
    public class RecognitionExceptionFilterAttribute : ExceptionFilterAttribute
    {
        public override Task OnExceptionAsync(ExceptionContext context)
        {
            return base.OnExceptionAsync(context);
        }
    }
}
