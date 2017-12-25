using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Text;
using EventStore.ClientAPI;
using Newtonsoft.Json;

namespace Evento.Repository
{
    public class EventStoreDomainRepositoryV2 : DomainRepositoryBaseV2
    {
        public readonly string Category;
        private readonly IEventStoreConnection _connection;

        public EventStoreDomainRepositoryV2(string category, IEventStoreConnection connection) 
        {
            Category = category;
            _connection = connection;
        }

        private string AggregateToStreamName(Type type, string id)
        {
            return $"{Category}-{type.Name}-{id}";
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
                SerializationUtils.DeserializeObject(e.OriginalEvent.Data, e.OriginalEvent.Metadata) as EventV2);
            return BuildAggregate<TResult>(deserializedEvents);
        }

        private EventData CreateEventData(object @event, string correlationId)
        {
            var metadata = new Dictionary<string, string>()
            {
                {
                    SerializationUtils.EventClrTypeHeader, @event.GetType().AssemblyQualifiedName
                },
                {
                    "Domain", Category
                },
                {
                    "$correlationId", correlationId
                }
            };
            var originalEventType = @event.GetType().Name;
            if (((EventV2)@event).Metadata != null)
            {
                if (((EventV2) @event).Metadata["Applies"] != null)
                {
                    metadata.Add("Applies", ((EventV2)@event).Metadata["Applies"].ToString(CultureInfo.InvariantCulture));
                }
                    
                if (((dynamic)@event).Metadata["Reverses"] != null)
                    metadata.Add("Reverses", ((dynamic)@event).Metadata["Reverses"].ToString());

                var tmp = ((IDictionary<string, object>) @event.ToDynamic());
                tmp.Remove("Metadata");
                @event = tmp;
            }
            var eventDataHeaders = SerializeObject(metadata);
            var data = SerializeObject(@event);
            var eventData = new EventData(Guid.NewGuid(), originalEventType, true, data, eventDataHeaders);
            return eventData;
        }

        private static byte[] SerializeObject(object obj)
        {
            var jsonObj = JsonConvert.SerializeObject(obj);
            var data = Encoding.UTF8.GetBytes(jsonObj);
            return data;
        }

        public override IEnumerable<EventV2> Save<TAggregate>(TAggregate aggregate) 
        {
            // Synchronous save operation
            var streamName = AggregateToStreamName(aggregate.GetType(), aggregate.AggregateId);
            var events = aggregate.UncommitedEvents().ToList();
            var originalVersion = CalculateExpectedVersion(aggregate, events);
            var expectedVersion = originalVersion == 0 ? ExpectedVersion.NoStream : originalVersion - 1;
            var eventData = events.Select(@event => CreateEventData(@event, aggregate.AggregateId)).ToArray();
            try
            {
                if (events.Count > 0)
                    _connection.AppendToStreamAsync(streamName, expectedVersion, eventData).Wait();
            }
            catch (AggregateException)
            {
                // Try to save using ExpectedVersion.Any to swallow silently WrongExpectedVersion error
                _connection.AppendToStreamAsync(streamName, ExpectedVersion.Any, eventData).Wait();
            }
            aggregate.ClearUncommitedEvents();
            return events;
        }
    }
}