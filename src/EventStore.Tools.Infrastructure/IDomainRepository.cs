using System.Collections.Generic;

namespace EventStore.Tools.Infrastructure
{
    public interface IDomainRepository
    {
        IEnumerable<DomainEvent> Save<TAggregate>(TAggregate aggregate) where TAggregate : IAggregate;
        TResult GetById<TResult>(string id) where TResult : IAggregate, new();
    }
}