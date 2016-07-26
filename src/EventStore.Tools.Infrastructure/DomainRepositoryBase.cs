using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventStore.Tools.Infrastructure
{
    public abstract class DomainRepositoryBase : IDomainRepository
    {
        public abstract IEnumerable<DomainEvent> Save<TAggregate>(TAggregate aggregate) where TAggregate : IAggregate;
        public abstract void Save(IEnumerable<object> events, string streamName);
        public abstract Task SaveAsynch<TAggregate>(TAggregate aggregate) where TAggregate : IAggregate;
        public abstract TResult GetById<TResult>(string id) where TResult : IAggregate, new();
        protected int CalculateExpectedVersion<T>(IAggregate aggregate, List<T> events)
        {
            var expectedVersion = aggregate.Version - events.Count;
            return expectedVersion;
        }
        protected TResult BuildAggregate<TResult>(IEnumerable<DomainEvent> events) where TResult : IAggregate, new()
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