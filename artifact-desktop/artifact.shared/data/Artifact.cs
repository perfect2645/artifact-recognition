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
        Processing,
        Completed,
        Cancelled,
        Failed
    }


    public record Artifact(
        string ArtifactId,
        string Name,
        string InputPath,
        string OutputPath,
        DateTime? UpdateTime,
        ArtifactStatus ArtifactStatus,
        RecognitionStatus RecognitionStatus,
        string? Comments
    );
}
