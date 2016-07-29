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


        private List<IEvent> _uncommitedEvents = new List<IEvent>();
        private Dictionary<Type, Action<IEvent>> _routes = new Dictionary<Type, Action<IEvent>>();
        private int version = -1;

        protected void RegisterTransition<T>(Action<T> transition) where T : class
        {
            _routes.Add(typeof(T), o => transition(o as T));
        }


        public void RaiseEvent(IEvent @event)
        {
            ApplyEvent(@event);
            _uncommitedEvents.Add(@event);
        }

        public void ApplyEvent(IEvent @event)
        {
            var eventType = @event.GetType();
            if (_routes.ContainsKey(eventType))
            {
                _routes[eventType](@event);
            }
            Version++;
        }

        public IEnumerable<IEvent> UncommitedEvents()
        {
            return _uncommitedEvents;
        }

        public void ClearUncommitedEvents()
        {
            _uncommitedEvents.Clear();
        }
    }
}
