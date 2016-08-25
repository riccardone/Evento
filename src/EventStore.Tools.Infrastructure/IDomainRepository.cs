using System.Collections.Generic;
using System.Threading.Tasks;
using EventStore.ClientAPI;

namespace EventStore.Tools.Infrastructure
{
    public interface IDomainRepository
    {
        /// <summary>
        /// This method write events asynchronusly and return a Task that can be used to wait and then do something example: youraggregate.CleanUncommittedEvents
        /// </summary>
        /// <typeparam name="TAggregate">IAggregate</typeparam>
        /// <param name="aggregate">The Aggregate containing uncommitted events</param>
        /// <returns>WriteResult from EventStore Client Api AppendToStreamAsync method</returns>
        Task<WriteResult> SaveAsync<TAggregate>(TAggregate aggregate) where TAggregate : IAggregate;

        /// <summary>
        /// This method save uncommitted events using an Async operation on the EventStore ClientApi but it doesn't wait the end of it and as side effect it doesn't clean the uncommitted events
        /// </summary>
        /// <param name="aggregate">The Aggregate containing uncommitted events</param>
        /// <returns>All the events that have been sent to EventStore</returns>
        /// <returns></returns>
        IEnumerable<IEvent> Save<TAggregate>(TAggregate aggregate) where TAggregate : IAggregate;

        /// <summary>
        /// This method can be used to retrieve the Aggregate in it's current state (all previous events applied)
        /// </summary>
        /// <typeparam name="TResult">IAggregate</typeparam>
        /// <param name="id">The unique identifier of the requested Aggregate</param>
        /// <returns>The Aggregate retrieved from the store in it's current state (all previous events applied)</returns>
        TResult GetById<TResult>(string id) where TResult : IAggregate, new();
    }
}