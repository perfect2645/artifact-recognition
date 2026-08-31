using artifact.service.domain.Hubs;
using artifact.shared.data;
using Microsoft.AspNetCore.SignalR;
using Utils.Ioc;

namespace artifact.service.domain.Services.Signalr
{
    [Register(ServiceType = typeof(IRealtimeService<ArtifactMessage>), Lifetime = Lifetime.Singleton)]
    public class RealtimeService(
        IHubContext<SignalRHub> hubContext,
        ILogger<RealtimeService> logger) : IRealtimeService<ArtifactMessage>
    {
        private readonly IHubContext<SignalRHub> _hubContext = hubContext;
        private readonly ILogger<RealtimeService> _logger = logger;

        public event Action<ArtifactMessage>? OnRealtimeMessageReceived;

        public async Task SendRealtimeAsync(string? eventName, ArtifactMessage payload, CancellationToken cancellationToken = default)
        {
            eventName = eventName ?? "ReceiveMessage";
            await _hubContext.Clients.All.SendAsync(eventName, payload, cancellationToken);
            _logger.LogInformation("SignalR: Sent message to all clients: {Payload}", @payload);
        }

        public async Task SendGroupRealtimeAsync(
            string groupName,
            string? eventName,
            ArtifactMessage payload, 
            CancellationToken cancellationToken = default)
        {
            eventName = eventName ?? "ReceiveMessage";
            await _hubContext.Clients.Group(groupName).SendAsync(eventName, payload, cancellationToken);
            _logger.LogInformation("SignalR: Sent message to group {GroupName}: {Payload}", groupName, @payload);
        }

        internal void RaiseOnRealtimeMessageReceived(ArtifactMessage payload)
        {
            OnRealtimeMessageReceived?.Invoke(payload);
            _logger.LogInformation("SignalR: Raised OnRealtimeMessageReceived event: {Payload}", @payload);
        }
    }
}
