using System;
using System.Collections.Generic;

namespace Evento
{
    public abstract class AggregateBaseV2 : IAggregateV2
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

        private List<EventV2> _uncommitedEvents = new List<EventV2>();
        private Dictionary<Type, Action<EventV2>> _routes = new Dictionary<Type, Action<EventV2>>();
        private int version = -1;

        protected void RegisterTransition<T>(Action<T> transition) where T : class
        {
            _routes.Add(typeof(T), o => transition(o as T));
        }

        public void RaiseEvent(EventV2 @event)
        {
            ApplyEvent(@event);
            _uncommitedEvents.Add(@event);
        }

        public void ApplyEvent(EventV2 @event)
        {
            var eventType = @event.GetType();
            if (_routes.ContainsKey(eventType))
            {
                _routes[eventType](@event);
            }
            Version++;
        }

        public IEnumerable<EventV2> UncommitedEvents()
        {
            return _uncommitedEvents;
        }

        public void ClearUncommitedEvents()
        {
            _uncommitedEvents.Clear();
        }
    }
}
