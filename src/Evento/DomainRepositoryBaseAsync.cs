using System.Collections.Generic;
using System.Threading.Tasks;

namespace Evento
{
    public abstract class DomainRepositoryBaseAsync : IDomainRepositoryAsync
    {        
        public abstract Task<IEnumerable<Event>> SaveAsync<TAggregate>(TAggregate aggregate) where TAggregate : IAggregate;
        public abstract Task<TResult> GetByIdAsync<TResult>(string id) where TResult : IAggregate, new();
        public abstract Task<TResult> GetByIdAsync<TResult>(string id, int eventsToLoad) where TResult : IAggregate, new();
        public abstract Task DeleteAggregateAsync<TAggregate>(string correlationId, bool hard);

        protected int CalculateExpectedVersion<T>(IAggregate aggregate, List<T> events)
        {
            var expectedVersion = aggregate.Version - events.Count;
            return expectedVersion;
        }

        protected async Task<TResult> BuildAggregate<TResult>(IEnumerable<Event> events) where TResult : IAggregate, new()
        {
            return await Task.Run(() =>
            {
                var result = new TResult();
                foreach (var @event in events)
                {
                    result.ApplyEvent(@event);
                }
                return result;
            });         
        }
    }
}