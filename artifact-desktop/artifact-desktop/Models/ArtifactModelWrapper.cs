using artifact.shared.data;

namespace artifact.desktop.Models
{
    internal static class ArtifactModelWrapper
    {
        extension(RecognitionItemUi recognitionItemUi)
        {
            public Artifact ToArtifact()
            {
                return new Artifact
                {
                    ArtifactId = recognitionItemUi.Id ?? Guid.NewGuid(),
                    Name = recognitionItemUi.Name,
                    InputPath = recognitionItemUi.InputPath,
                    OutputPath = recognitionItemUi.OutputPath ?? string.Empty,
                    UpdateTime = recognitionItemUi.UpdateTime ?? DateTime.Now,
                    ArtifactStatus = recognitionItemUi.ArtifactStatus,
                    RecognitionStatus = recognitionItemUi.RecognitionStatus,
                    Comments = recognitionItemUi.Comments
                };
            }
        }
    }
}
