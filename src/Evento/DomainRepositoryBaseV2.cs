using System.Collections.Generic;

namespace Evento
{
    public abstract class DomainRepositoryBaseV2 : IDomainRepositoryV2
    {
        public abstract IEnumerable<EventV2> Save<TAggregate>(TAggregate aggregate) where TAggregate : IAggregateV2;
        public abstract TResult GetById<TResult>(string id) where TResult : IAggregateV2, new();
        protected int CalculateExpectedVersion<T>(IAggregateV2 aggregate, List<T> events)
        {
            var expectedVersion = aggregate.Version - events.Count;
            return expectedVersion;
        }
        protected TResult BuildAggregate<TResult>(IEnumerable<EventV2> events) where TResult : IAggregateV2, new()
        {
            var result = new TResult();
            foreach (var @event in events)
            {
                result.ApplyEvent(@event);
            }
            return result;
        }
    }
}