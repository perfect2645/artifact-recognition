namespace artifact.shared.data
{
    public interface IRealTimeMessage<out TPayload> where TPayload : notnull
    {
        string Sender { get; }
        string Topic { get; }
        TPayload Message { get; }
    }
}
