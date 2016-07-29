using System.Collections.Generic;

namespace EventStore.Tools.Infrastructure
{
    public interface IAggregate
    {
        int Version { get; }
        string AggregateId { get; }
        void ApplyEvent(IEvent @event);
        IEnumerable<IEvent> UncommitedEvents();
        void ClearUncommitedEvents();
    }
}
