using artifact.shared.data;

namespace artifact.service.domain.Models.SignalR
{
    public record ArtifactMessage(string Sender, string Topic, Artifact Message) : IRealTimeMessage<Artifact>;
}
