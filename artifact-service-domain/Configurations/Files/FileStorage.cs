namespace artifact.service.domain.Configurations.Files
{
    public record FileStorage
    {
        public required string RootPath { get; init; }
    }
}
