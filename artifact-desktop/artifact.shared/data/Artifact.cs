namespace artifact.shared.data
{
    public enum ArtifactStatus
    {
        Unknown = 0,
        NoArtifact = 1,
        ArtifactExists = 2,
    }

    public enum RecognitionStatus
    {
        Pending = 0,
        Creating,
        Created,
        Processing,
        Completed,
        Cancelled,
        Failed
    }


    public record Artifact
    {
        public required Guid ArtifactId { get; init; }
        public required string Name { get; init; }
        public required string InputPath { get; init; }
        public string OutputPath { get; init; } = string.Empty;
        public DateTime? UpdateTime { get; init; }
        public ArtifactStatus ArtifactStatus { get; init; }
        public RecognitionStatus RecognitionStatus { get; init; }
        public string? Comments { get; init; }
    }
}
