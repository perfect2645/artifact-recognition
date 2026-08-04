namespace artifact.desktop.Configurations
{
    public record SignalRSettings
    {
        public string HubUrl { get; init; } = string.Empty;
        public required string Group { get; init; }
        public int MaxReconnectRetries { get; init; } = 5;
        public int HandshakeTimeout { get; init; } = 15;
    }
}
