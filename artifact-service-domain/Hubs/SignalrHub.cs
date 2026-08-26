using artifact.service.domain.Configurations;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace artifact.service.domain.Hubs
{
    public class SignalRHub(IOptions<SignalRSettings> signalRSettings,
        ILogger<SignalRHub> logger) : Hub
    {
        private readonly SignalRSettings _signalRSettings = signalRSettings.Value;
        private readonly ILogger<SignalRHub> _logger = logger;

        /// <summary>
        /// Client connected
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var connectionId = Context.ConnectionId;
            _logger.LogInformation("Client [{connectionId}] connected to SignalR", connectionId);
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Client disconnected
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;
            try
            {
                //await Groups.RemoveFromGroupAsync(connectionId, _signalRSettings.Group);

                if (exception != null)
                {
                    _logger.LogError(exception, "Client [{connectionId}] disconnected from SignalR unexpectly", connectionId);
                }
                else
                {
                    _logger.LogInformation("Client [{connectionId}] manually disconnected from SignalR", connectionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR connection error: {Message}", ex.Message);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinGroup(string groupName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(groupName);

            await Groups.AddToGroupAsync(Context.ConnectionId, groupName.Trim());
            _logger.LogInformation("SignalR: Client [{ConnectionId}] joined group : {GroupName}", Context.ConnectionId, groupName);
        }

        public async Task LeaveGroup(string groupName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(groupName);

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName.Trim());
            _logger.LogInformation("SignalR: Client [{ConnectionId}] left group : {GroupName}", Context.ConnectionId, groupName);
        }
    }
}
