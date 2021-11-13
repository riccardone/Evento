using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Evento
{
    /// <summary>
    /// This interface define the methods that a DomainRepository must expose
    /// </summary>
    public interface IDomainRepository
    {
        /// <summary>
        /// This synchronous method save all the uncommitted events of the passed aggregate 
        /// </summary>
        /// <param name="aggregate">The Aggregate containing uncommitted events</param>
        /// <returns>All the events that have been sent to EventStore</returns>
        IEnumerable<Event> Save<TAggregate>(TAggregate aggregate) where TAggregate : IAggregate;

        [Obsolete("Use IDomainRepositoryAsync interface for async methods")]
        /// <summary>
        /// This asynchronous method save all the uncommitted events of the passed aggregate 
        /// </summary>
        /// <param name="aggregate">The Aggregate containing uncommitted events</param>
        /// <returns>All the events that have been sent to EventStore</returns>
        Task<IEnumerable<Event>> SaveAsync<TAggregate>(TAggregate aggregate) where TAggregate : IAggregate;

        /// <summary>
        /// This method can be used to retrieve the Aggregate in it's current state (all previous events applied)
        /// </summary>
        /// <typeparam name="TResult">IAggregate</typeparam>
        /// <param name="correlationId">The unique identifier of the requested Aggregate</param>
        /// <returns>The Aggregate retrieved from the store in it's current state (all previous events applied)</returns>
        TResult GetById<TResult>(string correlationId) where TResult : IAggregate, new();

        /// <summary>
        /// This method can be used to retrieve the Aggregate in it's current state (all previous events applied)
        /// </summary>
        /// <typeparam name="TResult">IAggregate</typeparam>
        /// <param name="correlationId">The unique identifier of the requested Aggregate</param>
        /// <param name="eventsToLoad">The number of events to apply if you don't need to load the full history</param>
        /// <returns>The Aggregate retrieved from the store in it's current state (all previous events applied)</returns>
        TResult GetById<TResult>(string correlationId, int eventsToLoad) where TResult : IAggregate, new();

        /// <summary>
        /// This method can be used to remove an entire aggregate and all the events within its stream from the disk
        /// </summary>
        /// <param name="correlationId">The unique identifier of the requested Aggregate</param>
        /// <param name="hard">If true it will be impossible to recreate the stream with the same correlationId</param>
        void DeleteAggregate<TAggregate>(string correlationId, bool hard);
    }
}