namespace artifact.shared.data
{
    public interface IStringMessage : IRealTimeMessage<string>
    {
        bool HasError { get; }
        string? ErrorMessage { get; }
    }
}
