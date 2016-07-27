namespace EventStore.Tools.Infrastructure
{
    public interface IEvent : IMessage
    {
        string Id { get; }
    }
}
