namespace artifact.shared.data
{
    public record ArtifactMessage(string Sender, string Topic, Artifact Message) : IRealTimeMessage<Artifact>;
}
