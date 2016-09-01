using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Exceptions;
using Newtonsoft.Json;

namespace EventStore.Tools.Infrastructure
{
    public class InMemoryDomainRespository : DomainRepositoryBase
    {
        public Dictionary<string, List<string>> EventStore = new Dictionary<string, List<string>>();
        private readonly List<IEvent> _latestEvents = new List<IEvent>();
        private readonly JsonSerializerSettings _serializationSettings;

        public InMemoryDomainRespository()
        {
            _serializationSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            };
        }

        public override IEnumerable<IEvent> Save<TAggregate>(TAggregate aggregate)
        {
            var eventsToSave = aggregate.UncommitedEvents().ToList();
            var serializedEvents = eventsToSave.Select(Serialize).ToList();
            var expectedVersion = CalculateExpectedVersion(aggregate, eventsToSave);
            if (expectedVersion < 0)
            {
                EventStore.Add(aggregate.AggregateId, serializedEvents);
            }
            else
            {
                var existingEvents = EventStore[aggregate.AggregateId];
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

        private string Serialize(IEvent arg)
        {
            return JsonConvert.SerializeObject(arg, _serializationSettings);
        }

        public IEnumerable<IEvent> GetLatestEvents()
        {
            return _latestEvents;
        }

        public override TResult GetById<TResult>(string id)
        {
            if (EventStore.ContainsKey(id))
            {
                var events = EventStore[id];
                var deserializedEvents = events.Select(e => JsonConvert.DeserializeObject(e, _serializationSettings) as IEvent);
                return BuildAggregate<TResult>(deserializedEvents);
            }
            throw new AggregateNotFoundException("Could not found aggregate of type " + typeof(TResult) + " and id " + id);
        }

        public void AddEvents(Dictionary<string, IEnumerable<IEvent>> eventsForAggregates)
        {
            foreach (var eventsForAggregate in eventsForAggregates)
            {
                EventStore.Add(eventsForAggregate.Key, eventsForAggregate.Value.Select(Serialize).ToList());
            }
        }

        public override Task<WriteResult> SaveAsync<TAggregate>(TAggregate aggregate)
        {
            throw new NotImplementedException();
        }
    }
}
