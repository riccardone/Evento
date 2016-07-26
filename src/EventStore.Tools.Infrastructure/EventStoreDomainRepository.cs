using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Newtonsoft.Json;

namespace EventStore.Tools.Infrastructure
{
    public class EventStoreDomainRepository : DomainRepositoryBase
    {
        public readonly string Category;
        private readonly IEventStoreConnection _connection;

        public EventStoreDomainRepository(string category, IEventStoreConnection connection)
        {
            Category = category;
            _connection = connection;
        }

        private string AggregateToStreamName(Type type, string id)
        {
            return $"{Category}-{type.Name}-{id}";
        }

        public override void Save(IEnumerable<object> messages, string streamName)
        {
            var enumerable = messages as object[] ?? messages.ToArray();
            if (messages == null || !enumerable.Any())
                return;
            var events = enumerable.Select(CreateEventData).ToList();
            _connection.AppendToStreamAsync(streamName, -1, events);
        }

        public override IEnumerable<DomainEvent> Save<TAggregate>(TAggregate aggregate)
        {
            var events = aggregate.UncommitedEvents().ToList();
            var originalVersion = CalculateExpectedVersion(aggregate, events); //aggregate.Version - events.Count;
            var expectedVersion = originalVersion == -1 ? ExpectedVersion.NoStream : originalVersion;
            var eventData = events.Select(CreateEventData);
            var streamName = AggregateToStreamName(aggregate.GetType(), aggregate.AggregateId);
            if (events.Count > 0)
            {
                _connection.AppendToStreamAsync(streamName, expectedVersion, eventData);
            }
            return events;
        }

        public override Task SaveAsynch<TAggregate>(TAggregate aggregate)
        {
            var events = aggregate.UncommitedEvents().ToList();
            var originalVersion = CalculateExpectedVersion(aggregate, events); //aggregate.Version - events.Count;
            var expectedVersion = originalVersion == -1 ? ExpectedVersion.NoStream : originalVersion; //originalVersion == 0 ? ExpectedVersion.NoStream : originalVersion - 1;
            var eventData = events.Select(CreateEventData);
            var streamName = AggregateToStreamName(aggregate.GetType(), aggregate.AggregateId);
            if (events.Count > 0)
            {
                return _connection.AppendToStreamAsync(streamName, expectedVersion, eventData);
            }
            return null;
        }

        public override TResult GetById<TResult>(string id) 
        {
            var streamName = AggregateToStreamName(typeof(TResult), id);
            var eventsSlice = _connection.ReadStreamEventsForwardAsync(streamName, 0, 4096, false);
            if (eventsSlice.Result.Status == SliceReadStatus.StreamNotFound)
            {
                throw new AggregateNotFoundException("Could not found aggregate of type " + typeof(TResult) + " and id " + id);
            }
            var deserializedEvents = eventsSlice.Result.Events.Select(e =>
            {
                var metadata = SerializationUtils.DeserializeObject<Dictionary<string, string>>(e.OriginalEvent.Metadata);
                var eventData = SerializationUtils.DeserializeObject(e.OriginalEvent.Data, metadata[SerializationUtils.EventClrTypeHeader]);
                return eventData as DomainEvent;
            });
            return BuildAggregate<TResult>(deserializedEvents);
        }

        public EventData CreateEventData(object @event)
        {
            var eventHeaders = new Dictionary<string, string>()
            {
                {
                    SerializationUtils.EventClrTypeHeader, @event.GetType().AssemblyQualifiedName
                },
                {
                    "Domain", "Cds"
                }
            };
            var eventDataHeaders = SerializeObject(eventHeaders);
            var data = SerializeObject(@event);
            var eventData = new EventData(Guid.NewGuid(), @event.GetType().Name, true, data, eventDataHeaders);
            return eventData;
        }

        private byte[] SerializeObject(object obj)
        {
            var jsonObj = JsonConvert.SerializeObject(obj);
            var data = Encoding.UTF8.GetBytes(jsonObj);
            return data;
        }
    }
}