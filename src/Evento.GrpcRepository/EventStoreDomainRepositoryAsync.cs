using System.Text;
using EventStore.Client;
using Newtonsoft.Json;

namespace Evento.Repository
{
    public class EventStoreDomainRepositoryAsync : DomainRepositoryBaseAsync
    {
        EventStore.Client.EventStoreClient _connection;
        private string _category;

        public EventStoreDomainRepositoryAsync(string category, EventStoreClient eventStoreClient)
        {
            _connection = eventStoreClient;
            _category = category;
        }

        private string AggregateToStreamName(Type type, string id)
        {
            return $"{_category}-{type.Name}-{id}";
        }

        public override async Task DeleteAggregateAsync<TAggregate>(string correlationId, bool hard)
        {
            var streamName = AggregateToStreamName(typeof(TAggregate), correlationId);
            await _connection.DeleteAsync(streamName, StreamState.Any);
        }

        public override Task<TResult> GetByIdAsync<TResult>(string id)
        {
            throw new NotImplementedException();
        }

        public override async Task<TResult> GetByIdAsync<TResult>(string id, int eventsToLoad)
        {
            var streamName = AggregateToStreamName(typeof(TResult), id);
            var result = _connection.ReadStreamAsync(Direction.Forwards, streamName, 0, eventsToLoad);            
            if (await result.ReadState == ReadState.StreamNotFound)
                throw new AggregateNotFoundException("Could not found aggregate of type " + typeof(TResult) +
                                                     " and id " + id);
            var deserializedEvents = await result.Select(e =>
                SerializationUtils.DeserializeObject(e.OriginalEvent.Data, e.OriginalEvent.Metadata) as Event).ToListAsync();
            return await Task.Run(() => BuildAggregate<TResult>(deserializedEvents));            
        }

        public override async Task<IEnumerable<Event>> SaveAsync<TAggregate>(TAggregate aggregate)
        {
            var streamName = AggregateToStreamName(aggregate.GetType(), aggregate.AggregateId);
            var events = aggregate.UncommitedEvents().ToList();
            var originalVersion = CalculateExpectedVersion(aggregate, events);
            var expectedVersion = originalVersion == 0 ? int.MaxValue : originalVersion - 1;
            var eventData = events.Select(CreateEventData).ToArray();
            try
            {
                if (events.Count > 0)
                    await _connection.AppendToStreamAsync(streamName, new StreamRevision((ulong)expectedVersion), eventData);
            }
            catch (AggregateException)
            {
                // Try to save using ExpectedVersion.Any to swallow silently WrongExpectedVersion error
                await _connection.AppendToStreamAsync(streamName, StreamRevision.None, eventData);
            }
            aggregate.ClearUncommitedEvents();
            return events;
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
            var eventData = new EventData(new Uuid(), originalEventType, data, eventDataHeaders);
            return eventData;
        }

        private static byte[] SerializeObject(object obj)
        {
            var jsonObj = JsonConvert.SerializeObject(obj);
            var data = Encoding.UTF8.GetBytes(jsonObj);
            return data;
        }
    }
}