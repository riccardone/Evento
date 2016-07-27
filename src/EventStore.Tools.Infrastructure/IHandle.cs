namespace EventStore.Tools.Infrastructure
{
    public interface IHandle<in TMessage> where TMessage : IMessage
    {
        IAggregate Handle(TMessage command);
    }
}
