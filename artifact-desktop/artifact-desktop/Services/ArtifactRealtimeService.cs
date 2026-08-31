using artifact.desktop.Generic;
using artifact.desktop.Messaging.Signalr;
using artifact.shared;
using artifact.shared.data;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Extensions.Hosting;
using Utils.Ioc;

namespace artifact.desktop.Services
{
    [Register(ServiceType = typeof(IHostedService), Key = Constants.ArtifactRealtimeService, Lifetime = Lifetime.Singleton)]
    public class ArtifactRealtimeService(
        ISignalRClient<ArtifactMessage> signalRClient,
        IMessenger messenger) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await signalRClient.StartAsync(cancellationToken);
            await signalRClient.JoinGroupAsync(SharedConstants.SignalrClientGroup, cancellationToken);
            signalRClient.MessageReceived += OnArtifactMessageReceived;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await signalRClient.StopAsync(cancellationToken);
            signalRClient.MessageReceived -= OnArtifactMessageReceived;
        }

        private void OnArtifactMessageReceived(ArtifactMessage message)
        {
            messenger.Send(new ValueChangedMessage<ArtifactMessage>(message));
        }
    }
}
