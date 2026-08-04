using artifact.shared.data;
using Microsoft.AspNetCore.SignalR.Client;

namespace artifact.desktop.Messaging.Signalr
{
    public interface ISignalRClient<TPayload> : IAsyncDisposable where TPayload : notnull
    {
        /// <summary>
        /// Gets the current state of the hub connection.
        /// </summary>
        HubConnectionState CurrentState { get; }

        event Action<HubConnectionState> StateChanged;

        event Action<IRealTimeMessage<TPayload>> MessageReceived;

        /// <summary>
        /// Starts the hub connection asynchronously.
        /// </summary>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops the hub connection asynchronously.
        /// </summary>
        Task StopAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a chat message to the specified target user.
        /// </summary>
        Task SendMessageAsync(IRealTimeMessage<TPayload> message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Joins the specified group.
        /// </summary>
        Task JoinGroupAsync(string groupName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Leaves the specified group.
        /// </summary>
        Task LeaveGroupAsync(string groupName, CancellationToken cancellationToken = default);
    }
}
