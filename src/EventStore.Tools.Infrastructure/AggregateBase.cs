using System;
using System.Collections.Generic;

namespace EventStore.Tools.Infrastructure
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


        private List<DomainEvent> _uncommitedEvents = new List<DomainEvent>();
        private Dictionary<Type, Action<DomainEvent>> _routes = new Dictionary<Type, Action<DomainEvent>>();
        private int version = -1;

        protected void RegisterTransition<T>(Action<T> transition) where T : class
        {
            _routes.Add(typeof(T), o => transition(o as T));
        }


        public void RaiseEvent(DomainEvent @event)
        {
            ApplyEvent(@event);
            _uncommitedEvents.Add(@event);
        }

        public void ApplyEvent(DomainEvent @event)
        {
            var eventType = @event.GetType();
            if (_routes.ContainsKey(eventType))
            {
                _routes[eventType](@event);
            }
            Version++;
        }

        public IEnumerable<DomainEvent> UncommitedEvents()
        {
            return _uncommitedEvents;
        }

        public void ClearUncommitedEvents()
        {
            _uncommitedEvents.Clear();
        }
    }
}
