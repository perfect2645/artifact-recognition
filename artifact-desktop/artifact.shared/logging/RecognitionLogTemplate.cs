using artifact.shared.data;
using Microsoft.Extensions.Logging;

namespace artifact.shared.logging
{
    public static partial class RecognitionLogTemplate
    {
        [LoggerMessage(EventId = (int)RecognitionStatus.Processing,
            Level = LogLevel.Information,
            Message = "Recognition task {taskId} started.")]
        public static partial void LogRecognitionProcessing(this ILogger logger, string taskId);
    }
}
