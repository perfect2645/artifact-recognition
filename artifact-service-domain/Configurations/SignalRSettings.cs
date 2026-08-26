namespace artifact.service.domain.Configurations
{
    public record SignalRSettings()
    {
        public string HubUrl { get; init; } = "signalr";
    }
}
