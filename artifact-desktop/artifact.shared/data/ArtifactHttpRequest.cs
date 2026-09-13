namespace artifact.shared.data
{
    public record ArtifactHttpRequest
    {
        public required Guid TaskId { get; init; }
        public required string FolderPath { get; init; }
        public required IReadOnlyList<Artifact> Artifacts { get; init; }
    }
}
