namespace artifact.service.domain.Services.Signalr
{
    public interface IRealtimeService<TPayload>
    {
        Task SendRealtimeAsync(TPayload payload, CancellationToken cancellationToken = default);
        Task SendGroupRealtimeAsync(string groupName, TPayload payload, CancellationToken cancellationToken = default);

        event Action<TPayload>? OnRealtimeMessageReceived;
    }
}
