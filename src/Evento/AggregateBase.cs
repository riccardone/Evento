using System;
using System.Collections.Generic;

namespace Evento
{
    public abstract class AggregateBase : IAggregate
    {
        public abstract string AggregateId { get; }
        public int Version
        {
            get
            {
                return version;
            }
            protected set
            {
                version = value;
            }
        }

        private List<Event> _uncommitedEvents = new List<Event>();
        private Dictionary<Type, Action<Event>> _routes = new Dictionary<Type, Action<Event>>();
        private int version = -1;

        protected void RegisterTransition<T>(Action<T> transition) where T : class
        {
            _routes.Add(typeof(T), o => transition(o as T));
        }

        public void RaiseEvent(Event @event)
        {
            ApplyEvent(@event);
            _uncommitedEvents.Add(@event);
        }

        public void ApplyEvent(Event @event)
        {
            var eventType = @event.GetType();
            if (_routes.ContainsKey(eventType))
            {
                _routes[eventType](@event);
            }
            Version++;
        }

        public IEnumerable<Event> UncommitedEvents()
        {
            return _uncommitedEvents;
        }

        public void ClearUncommitedEvents()
        {
            _uncommitedEvents.Clear();
        }
    }
}
