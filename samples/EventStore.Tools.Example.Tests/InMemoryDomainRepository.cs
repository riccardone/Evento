using System.Collections.Generic;
using System.Linq;
using EventStore.ClientAPI.Exceptions;
using EventStore.Tools.Infrastructure;
using Newtonsoft.Json;

namespace EventStore.Tools.Example.Tests
{
    /// <summary>
    /// This DomainRepository can be used for tests
    /// </summary>
    public class InMemoryDomainRespository : DomainRepositoryBase
    {
        public Dictionary<string, List<string>> EventStore = new Dictionary<string, List<string>>();
        private readonly List<Event> _latestEvents = new List<Event>();
        private readonly JsonSerializerSettings _serializationSettings;

        public InMemoryDomainRespository()
        {
            _serializationSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            };
        }
        public override IEnumerable<Event> Save<TAggregate>(TAggregate aggregate, string correlationId)
        {
            var eventsToSave = aggregate.UncommitedEvents().ToList();
            var serializedEvents = eventsToSave.Select(Serialize).ToList();
            var expectedVersion = CalculateExpectedVersion(aggregate, eventsToSave);
            if (expectedVersion < 0)
            {
                EventStore.Add(correlationId, serializedEvents);
            }
            else
            {
                var existingEvents = EventStore[correlationId];
                var currentversion = existingEvents.Count - 1;
                if (currentversion != expectedVersion)
                {
                    throw new WrongExpectedVersionException("Expected version " + expectedVersion +
                                                            " but the version is " + currentversion);
                }
                existingEvents.AddRange(serializedEvents);
            }
            _latestEvents.AddRange(eventsToSave);
            aggregate.ClearUncommitedEvents();
            return eventsToSave;
        }

        private string Serialize(Event arg)
        {
            return JsonConvert.SerializeObject(arg, _serializationSettings);
        }

        public IEnumerable<Event> GetLatestEvents()
        {
            return _latestEvents;
        }

        public override TResult GetById<TResult>(string id)
        {
            if (EventStore.ContainsKey(id))
            {
                var events = EventStore[id];
                var deserializedEvents = events.Select(e => JsonConvert.DeserializeObject(e, _serializationSettings) as Event);
                return BuildAggregate<TResult>(deserializedEvents);
            }
            throw new AggregateNotFoundException("Could not found aggregate of type " + typeof(TResult) + " and id " + id);
        }

        public void AddEvents(Dictionary<string, IEnumerable<Event>> eventsForAggregates)
        {
            foreach (var eventsForAggregate in eventsForAggregates)
            {
                EventStore.Add(eventsForAggregate.Key, eventsForAggregate.Value.Select(Serialize).ToList());
            }
        }
    }
}