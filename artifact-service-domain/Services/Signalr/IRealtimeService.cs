namespace artifact.service.domain.Services.Signalr
{
    public interface IRealtimeService<TPayload>
    {
        Task SendRealtimeAsync(string? eventName, TPayload payload, CancellationToken cancellationToken = default);
        Task SendGroupRealtimeAsync(string groupName, string? eventName, TPayload payload, CancellationToken cancellationToken = default);

        event Action<TPayload>? OnRealtimeMessageReceived;
    }
}
