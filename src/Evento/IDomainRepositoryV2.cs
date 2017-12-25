using System.Collections.Generic;

namespace Evento
{
    /// <summary>
    /// This interface define the methods that a DomainRepository must expose
    /// </summary>
    public interface IDomainRepositoryV2
    {
        /// <summary>
        /// This synchronous method save all the uncommitted events of the passed aggregate 
        /// </summary>
        /// <param name="aggregate">The Aggregate containing uncommitted events</param>
        /// <returns>All the events that have been sent to EventStore</returns>
        IEnumerable<EventV2> Save<TAggregate>(TAggregate aggregate) where TAggregate : IAggregateV2;

        /// <summary>
        /// This method can be used to retrieve the Aggregate in it's current state (all previous events applied)
        /// </summary>
        /// <typeparam name="TResult">IAggregate</typeparam>
        /// <param name="correlationId">The unique identifier of the requested Aggregate</param>
        /// <returns>The Aggregate retrieved from the store in it's current state (all previous events applied)</returns>
        TResult GetById<TResult>(string correlationId) where TResult : IAggregateV2, new();
    }
}