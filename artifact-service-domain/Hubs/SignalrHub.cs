using artifact.service.domain.Configurations;
using Logging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace artifact.service.domain.Hubs
{
    public class SignalRHub(IOptions<SignalRSettings> signalRSettings) : Hub
    {
        private readonly SignalRSettings _signalRSettings = signalRSettings.Value;

        /// <summary>
        /// Client connected
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var connectionId = Context.ConnectionId;
            Log4Logger.Logger.Info($"Client [{connectionId}] connected to SignalR");

            await Groups.AddToGroupAsync(connectionId, _signalRSettings.Group);

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
                await Groups.RemoveFromGroupAsync(connectionId, _signalRSettings.Group);

                if (exception != null)
                {
                    Log4Logger.Logger.Error($"Client [{connectionId}] disconnected from SignalR unexpectly", exception);
                }
                else
                {
                    Log4Logger.Logger.Info($"Client [{connectionId}] manually disconnected from SignalR");
                }
            }
            catch (Exception ex)
            {
                Log4Logger.Logger.Error($"SignalR connection error: {ex.Message}", ex);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinGroup(string groupName)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(groupName);

            await Groups.AddToGroupAsync(Context.ConnectionId, groupName.Trim());
            Log4Logger.Logger.Info($"SignalR: Client [{Context.ConnectionId}] joined group : {groupName}");
        }

        public async Task LeaveGroup(string groupName)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(groupName);

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName.Trim());
            Log4Logger.Logger.Info($"SignalR: Client[{Context.ConnectionId}] leaved group : {groupName}");
        }
    }
}
