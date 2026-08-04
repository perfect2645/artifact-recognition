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
        Recognized = 1,
        Canceled = 2,
        Error = 3,
    }


    public record Artifact(
        string ArtifactId,
        string Name,
        DateTime? UpdateTime,
        ArtifactStatus ArtifactStatus,
        RecognitionStatus RecognitionStatus
    );
}
