using System.Text;
using KurrentDB.Client;
using Newtonsoft.Json;

namespace Evento.Repository.Grpc
{
    public class EventStoreDomainRepository : DomainRepositoryBase
    {
        readonly KurrentDBClient _connection;
        private readonly string _category;

        public EventStoreDomainRepository(string category, KurrentDBClient eventStoreClient)
        {
            _connection = eventStoreClient;
            _category = category;
        }

        private string AggregateToStreamName(Type type, string id)
        {
            return $"{_category}-{type.Name}-{id}";
        }

        public override TResult GetById<TResult>(string id, int eventsToLoad)
        {
            var streamName = AggregateToStreamName(typeof(TResult), id);
            var result = _connection.ReadStreamAsync(Direction.Forwards, streamName, 0, eventsToLoad);
            if (result.ReadState.Result == ReadState.StreamNotFound)
                throw new AggregateNotFoundException("Could not found aggregate of type " + typeof(TResult) +
                                                     " and id " + id);
            var deserializedEvents = result.Select(e =>
                SerializationUtils.DeserializeObject(e.OriginalEvent.Data, e.OriginalEvent.Metadata) as Event).ToListAsync().Result;
            return BuildAggregate<TResult>(deserializedEvents);
        }

        public override void DeleteAggregate<TAggregate>(string correlationId, bool hard)
        {
            var streamName = AggregateToStreamName(typeof(TAggregate), correlationId);
            _connection.DeleteAsync(streamName, StreamState.Any).Wait();
        }

        public override TResult GetById<TResult>(string correlationId)
        {
            var streamName = AggregateToStreamName(typeof(TResult), correlationId);
            var result = _connection.ReadStreamAsync(Direction.Forwards, streamName, 0);
            if (result.ReadState.Result == ReadState.StreamNotFound)
                throw new AggregateNotFoundException("Could not found aggregate of type " + typeof(TResult) +
                                                     " and id " + correlationId);
            var deserializedEvents = result.Where(x=>x.Event.EventType != "Test").Select(e =>
                SerializationUtils.DeserializeObject(e.OriginalEvent.Data, e.OriginalEvent.Metadata) as Event).ToListAsync().Result;
            return BuildAggregate<TResult>(deserializedEvents);
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
            var eventData = new EventData(Uuid.NewUuid(), originalEventType, data, eventDataHeaders);
            return eventData;
        }

        private static byte[] SerializeObject(object obj)
        {
            var jsonObj = JsonConvert.SerializeObject(obj);
            var data = Encoding.UTF8.GetBytes(jsonObj);
            return data;
        }

        public override IEnumerable<Event> Save<TAggregate>(TAggregate aggregate)
        {
            var streamName = AggregateToStreamName(aggregate.GetType(), aggregate.AggregateId);
            var events = aggregate.UncommitedEvents().ToList();
            // TODO verify if we still need to calculate the version using grpc client
            //var originalVersion = CalculateExpectedVersion(aggregate, events);
            //var expectedVersion = originalVersion == 0 ? int.MaxValue : originalVersion - 1;
            var eventData = events.Select(CreateEventData).ToArray();

            if (events.Count > 0)
                _connection.AppendToStreamAsync(streamName, StreamState.Any, eventData).Wait();

            aggregate.ClearUncommitedEvents();
            return events;
        }

        [Obsolete("use the async repo")]
        public override async Task<IEnumerable<Event>> SaveAsync<TAggregate>(TAggregate aggregate)
        {
            throw new NotImplementedException("use the async repo");
        }
    }
}