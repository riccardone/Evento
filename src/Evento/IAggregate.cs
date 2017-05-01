using System.Collections.Generic;

namespace Evento
{
    /// <summary>
    /// An interface to create a class that group toghether behaviours/methods related to a business feature
    /// </summary>
    public interface IAggregate
    {
        int Version { get; }
        /// <summary>
        /// The unique identifier of the Aggregate
        /// </summary>
        string AggregateId { get; }
        /// <summary>
        /// This method is used to apply and obtains the current state of an Aggregate instance
        /// </summary>
        /// <param name="event"></param>
        void ApplyEvent(Event @event);
        /// <summary>
        /// The list of all Event that are not yet committed to a store
        /// </summary>
        /// <returns>The list of all the uncommitted events</returns>
        IEnumerable<Event> UncommitedEvents();
        /// <summary>
        /// Method used to clearup the uncommitted event after a succesful save operation
        /// </summary>
        void ClearUncommitedEvents();
    }
}
