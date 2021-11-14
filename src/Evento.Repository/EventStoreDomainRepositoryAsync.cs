using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Newtonsoft.Json;

namespace Evento.Repository
{
    public class EventStoreDomainRepositoryAsync : DomainRepositoryBaseAsync
    {
        public readonly string Category;
        private readonly IEventStoreConnection _connection;

        public EventStoreDomainRepositoryAsync(string category, IEventStoreConnection connection)
        {
            Category = category;
            _connection = connection;
        }

        private string AggregateToStreamName(Type type, string id)
        {
            return $"{Category}-{type.Name}-{id}";
        }

        public override async Task<TResult> GetByIdAsync<TResult>(string id, int eventsToLoad)
        {
            var streamName = AggregateToStreamName(typeof(TResult), id);
            var eventsSlice = await _connection.ReadStreamEventsForwardAsync(streamName, 0, eventsToLoad, false);
            if (eventsSlice.Status == SliceReadStatus.StreamNotFound)
                throw new AggregateNotFoundException("Could not found aggregate of type " + typeof(TResult) +
                                                     " and id " + id);
            var deserializedEvents = await Task.Run(() => eventsSlice.Events.Select(e =>
                SerializationUtils.DeserializeObject(e.OriginalEvent.Data, e.OriginalEvent.Metadata) as Event));
            return await Task.Run(() => BuildAggregate<TResult>(deserializedEvents));
        }

        public override async Task DeleteAggregateAsync<TAggregate>(string correlationId, bool hard)
        {
            var streamName = AggregateToStreamName(typeof(TAggregate), correlationId);
            await _connection.DeleteStreamAsync(streamName, ExpectedVersion.Any, hard);
        }

        public override async Task<TResult> GetByIdAsync<TResult>(string correlationId)
        {
            var streamName = AggregateToStreamName(typeof(TResult), correlationId);
            var streamEvents = new List<ResolvedEvent>();
            StreamEventsSlice currentSlice;
            var nextSliceStart = StreamPosition.Start;
            do
            {
                currentSlice = await _connection.ReadStreamEventsForwardAsync(streamName, nextSliceStart, 200, false);
                if (currentSlice.Status == SliceReadStatus.StreamNotFound)
                    throw new AggregateNotFoundException("Could not found aggregate of type " + typeof(TResult) +
                                                         " and id " + correlationId);
                nextSliceStart = (int)currentSlice.NextEventNumber;
                streamEvents.AddRange(currentSlice.Events);
            } while (!currentSlice.IsEndOfStream);
            var deserializedEvents = await Task.Run(() => streamEvents.Select(e =>
                SerializationUtils.DeserializeObject(e.Event.Data, e.Event.Metadata) as Event));
            return await Task.Run(() => BuildAggregate<TResult>(deserializedEvents));
        }

        private static EventData CreateEventData(object @event)
        {
            IDictionary<string, string> metadata;
            var originalEventType = @event.GetType().Name;
            if (((Event)@event).Metadata != null)
            {
                metadata = ((Event)@event).Metadata;
                if (!metadata.ContainsKey("$correlationId"))
                    throw new Exception("The event metadata must contains a $correlationId");
                if (!metadata.ContainsKey(SerializationUtils.EventClrTypeHeader))
                    metadata.Add(SerializationUtils.EventClrTypeHeader, @event.GetType().AssemblyQualifiedName);
                else
                    metadata[SerializationUtils.EventClrTypeHeader] = @event.GetType().AssemblyQualifiedName;
                // Remove the metadata from the event body
                var tmp = (IDictionary<string, object>)@event.ToDynamic();
                tmp.Remove("Metadata");
                @event = tmp;
            }
            else
                throw new Exception("The event must have a $correlationId present in its metadata");
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

        public override async Task<IEnumerable<Event>> SaveAsync<TAggregate>(TAggregate aggregate)
        {            
            var streamName = AggregateToStreamName(aggregate.GetType(), aggregate.AggregateId);
            var events = aggregate.UncommitedEvents().ToList();
            var originalVersion = CalculateExpectedVersion(aggregate, events);
            var expectedVersion = originalVersion == 0 ? ExpectedVersion.NoStream : originalVersion - 1;
            var eventData = await Task.Run(() => events.Select(CreateEventData).ToArray());
            try
            {
                if (events.Count > 0)
                    await _connection.AppendToStreamAsync(streamName, expectedVersion, eventData);
            }
            catch (AggregateException)
            {
                // Try to save using ExpectedVersion.Any to swallow silently WrongExpectedVersion error
                await _connection.AppendToStreamAsync(streamName, ExpectedVersion.Any, eventData);
            }
            aggregate.ClearUncommitedEvents();
            return events;
        }
    }
}