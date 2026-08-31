using artifact.desktop.Configurations;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text.Json;
using Utils.Tasking;

namespace artifact.desktop.Messaging.Signalr
{
    public sealed class SignalRClient<TPayload> : ISignalRClient<TPayload>, IHostedService where TPayload : notnull
    {
        private readonly HubConnection _hubConnection;
        private readonly ILogger<SignalRClient<TPayload>> _logger;
        private readonly SignalRSettings _settings;

        // Persistent group set: automatically restored after reconnection
        private readonly HashSet<string> _joinedGroups = new();
        private readonly Lock _groupLock = new();

        /// <inheritdoc />
        public HubConnectionState CurrentState => _hubConnection.State;

        /// <inheritdoc />
        public event Action<HubConnectionState>? StateChanged;

        /// <inheritdoc />
        public event Action<TPayload>? MessageReceived;

        /// <summary>
        /// Initializes a new instance. Performs only lightweight setup; no network I/O.
        /// </summary>
        public SignalRClient(
            IOptions<SignalRSettings> settings,
            ILogger<SignalRClient<TPayload>> logger)
        {
            _settings = settings.Value;
            _logger = logger;

            if (string.IsNullOrWhiteSpace(_settings.HubUrl))
                throw new ArgumentException("SignalR Hub URL is not configured");
            _hubConnection = BuildHubConnection();

            // Register connection lifecycle event handlers
            RegisterLifecycleEvents();
            // Register server-to-client message handlers
            RegisterMessageHandlers();
        }

        private HubConnection BuildHubConnection()
        {

            // Build hub connection with full configuration
            return new HubConnectionBuilder()
                .WithUrl(_settings.HubUrl, options =>
                {
                    // Dynamic access token provider: replace with your auth service
                    options.AccessTokenProvider = async () =>
                    {
                        // return await AuthService.GetValidAccessTokenAsync();
                        return await Task.FromResult(string.Empty);
                    };

                    // Bypass self-signed certificate validation in development → REMOVE in production
                    options.HttpMessageHandlerFactory = handler =>
                    {
                        if (handler is HttpClientHandler clientHandler)
                        {
                            clientHandler.ServerCertificateCustomValidationCallback =
                                (_, _, _, _) => true;
                        }
                        return handler;
                    };
                })
                // Step-based automatic reconnection policy
                .WithAutomaticReconnect(
                [
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(20)
                ])
                // Keep serialization rules fully aligned with the server (camelCase)
                .AddJsonProtocol(options =>
                {
                    options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                })
                // Integrate with host logging system
                .ConfigureLogging(logging =>
                {
                    logging.SetMinimumLevel(LogLevel.Information);
                })
                .Build();
        }

        #region Internal Registration
        /// <summary>
        /// Subscribes to all connection state lifecycle events.
        /// </summary>
        private void RegisterLifecycleEvents()
        {
            // Fired when the connection drops and automatic reconnection starts
            _hubConnection.Reconnecting += error =>
            {
                _logger.LogWarning(error, "SignalR connection lost. Automatic reconnection started.");
                RaiseStateChanged();
                return Task.CompletedTask;
            };

            // Fired when reconnection completes successfully
            _hubConnection.Reconnected += connectionId =>
            {
                _logger.LogInformation("SignalR reconnected. New connection ID: {ConnectionId}", connectionId);
                _ = RestoreGroupsAsync();
                RaiseStateChanged();
                return Task.CompletedTask;
            };

            // Fired when connection is closed intentionally or reconnection is exhausted
            _hubConnection.Closed += error =>
            {
                if (error == null)
                    _logger.LogInformation("SignalR connection closed gracefully.");
                else
                    _logger.LogError(error, "SignalR connection terminated abnormally.");

                RaiseStateChanged();
                return Task.CompletedTask;
            };
        }

        /// <summary>
        /// Registers all server-invokable client methods.
        /// </summary>
        private void RegisterMessageHandlers()
        {
            // Incoming chat message handler
            _hubConnection.On<TPayload>("ReceiveMessage", OnMessageReceived);

            // System-wide notification handler
            _hubConnection.On<string>("SystemNotice", notice =>
            {
                _logger.LogDebug("System notice received: {Notice}", notice);
                // Extend with UI presentation logic as needed
            });
        }

        private void OnMessageReceived(TPayload message)
        {
            _logger.LogDebug("Signalr message received : {@message}", message);
            MessageReceived?.Invoke(message);
        }

        /// <summary>
        /// Rejoins all previously joined groups after a successful reconnection.
        /// </summary>
        private async Task RestoreGroupsAsync()
        {
            string[] groups;
            lock (_groupLock)
            {
                groups = _joinedGroups.ToArray();
            }

            foreach (var group in groups)
            {
                try
                {
                    await _hubConnection.InvokeAsync("JoinGroup", group);
                    _logger.LogInformation("Group membership restored: {Group}", group);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to restore group membership: {Group}", group);
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="StateChanged"/> event on the UI thread.
        /// </summary>
        private void RaiseStateChanged()
        {
            StateChanged?.Invoke(CurrentState);
        }
        #endregion

        #region Public Business Methods
        /// <inheritdoc />
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (CurrentState is HubConnectionState.Connected
                or HubConnectionState.Connecting
                or HubConnectionState.Reconnecting)
                return;

            try
            {
                _logger.LogInformation("Establishing SignalR connection to {HubUrl}", _settings.HubUrl);
                await _hubConnection.StartAsync(cancellationToken);
                _logger.LogInformation("SignalR connected. Connection ID: {ConnectionId}", _hubConnection.ConnectionId);
                RaiseStateChanged();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to establish SignalR connection.");
                RaiseStateChanged();
                throw;
            }
        }

        /// <inheritdoc />
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (CurrentState == HubConnectionState.Disconnected)
                return;

            try
            {
                _logger.LogInformation("Closing SignalR connection...");
                await _hubConnection.StopAsync(cancellationToken);
                _logger.LogInformation("SignalR connection closed.");
                RaiseStateChanged();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while closing SignalR connection.");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task SendMessageAsync(TPayload message, CancellationToken cancellationToken = default)
        {
            // Auto-connect if not already connected
            if (CurrentState != HubConnectionState.Connected)
                await StartAsync(cancellationToken);

            try
            {
                await _hubConnection.InvokeAsync("SendMessage", new object[] { message }, cancellationToken);
                _logger.LogDebug("SignalR message sent: {message}", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SignalR message: {message}", message);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task JoinGroupAsync(string groupName, CancellationToken cancellationToken = default)
        {
            if (CurrentState != HubConnectionState.Connected)
                await StartAsync(cancellationToken);

            await _hubConnection.InvokeAsync("JoinGroup", groupName, cancellationToken);

            lock (_groupLock)
            {
                _joinedGroups.Add(groupName);
            }

            _logger.LogInformation("Joined group: {Group}", groupName);
        }

        /// <inheritdoc />
        public async Task LeaveGroupAsync(string groupName, CancellationToken cancellationToken = default)
        {
            if (CurrentState == HubConnectionState.Connected)
            {
                await _hubConnection.InvokeAsync("LeaveGroup", new object[] { groupName }, cancellationToken);
            }

            lock (_groupLock)
            {
                _joinedGroups.Remove(groupName);
            }

            _logger.LogInformation("Left group: {Group}", groupName);
        }
        #endregion

        #region Disposal
        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
            }
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
