using System.Collections.Generic;

namespace EventStore.Tools.Infrastructure
{
    public abstract class DomainRepositoryBase : IDomainRepository
    {
        public abstract IEnumerable<Event> Save<TAggregate>(TAggregate aggregate) where TAggregate : IAggregate;
        public abstract TResult GetById<TResult>(string id) where TResult : IAggregate, new();
        protected int CalculateExpectedVersion<T>(IAggregate aggregate, List<T> events)
        {
            var expectedVersion = aggregate.Version - events.Count;
            return expectedVersion;
        }
        protected TResult BuildAggregate<TResult>(IEnumerable<Event> events) where TResult : IAggregate, new()
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