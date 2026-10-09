namespace artifact.shared
{
    public static class SharedConstants
    {
        public const string SignalREndpoint = "/hubs/signalr";
        public const string SignalrClientGroup = "ui";
        public const string SignalrRecognitionModelGroup = "recognition-model";
        public const string SignalrClientReceiveMessage = "ReceiveMessage";

        #region Http request

        public const string HttpTaskIdHeader = "X-Task-Id";

        #endregion Http request
    }
}
