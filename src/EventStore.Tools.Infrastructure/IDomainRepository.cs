using System.Collections.Generic;

namespace EventStore.Tools.Infrastructure
{
    /// <summary>
    /// This interface define the methods that a DomainRepository must expose
    /// </summary>
    public interface IDomainRepository
    {
        /// <summary>
        /// This method save uncommitted events using an Async operation on the EventStore ClientApi but it doesn't wait the end of it and as side effect it doesn't clean the uncommitted events
        /// </summary>
        /// <param name="aggregate">The Aggregate containing uncommitted events</param>
        /// <param name="correlationId">The id that links the events toghether in the same stream</param>
        /// <returns>All the events that have been sent to EventStore</returns>
        IEnumerable<Event> Save<TAggregate>(TAggregate aggregate, string correlationId) where TAggregate : IAggregate;

        /// <summary>
        /// This method can be used to retrieve the Aggregate in it's current state (all previous events applied)
        /// </summary>
        /// <typeparam name="TResult">IAggregate</typeparam>
        /// <param name="correlationId">The unique identifier of the requested Aggregate</param>
        /// <returns>The Aggregate retrieved from the store in it's current state (all previous events applied)</returns>
        TResult GetById<TResult>(string correlationId) where TResult : IAggregate, new();
    }
}