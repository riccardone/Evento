using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Evento.Repository
{
    public class EventStoreDomainRepository : DomainRepositoryBase
    {
        public readonly string Category;
        private readonly IEventStoreConnection _connection;
        private readonly IKeyReader _keyReader;
        private readonly ICryptoService _cryptoService;

        public EventStoreDomainRepository(string category, IEventStoreConnection connection)
        {
            Category = category;
            _connection = connection;
        }

        public EventStoreDomainRepository(string category, IEventStoreConnection connection, IKeyReader keyReader,
            ICryptoService cryptoService)
        {
            Category = category;
            _connection = connection;
            _keyReader = keyReader;
            _cryptoService = cryptoService;
        }

        private string AggregateToStreamName(Type type, string id)
        {
            return $"{Category}-{type.Name}-{id}";
        }

        public override TResult GetById<TResult>(string id, int eventsToLoad)
        {
            var streamName = AggregateToStreamName(typeof(TResult), id);
            var eventsSlice = _connection.ReadStreamEventsForwardAsync(streamName, 0, eventsToLoad, false);
            if (eventsSlice.Result.Status == SliceReadStatus.StreamNotFound)
                throw new AggregateNotFoundException("Could not found aggregate of type " + typeof(TResult) +
                                                     " and id " + id);
            var deserializedEvents = eventsSlice.Result.Events.Select(e =>
                DeserializeObject(e.OriginalEvent.Data, e.OriginalEvent.Metadata) as Event);
            return BuildAggregate<TResult>(deserializedEvents);
        }

        public override void DeleteAggregate<TAggregate>(string correlationId, bool hard)
        {
            var streamName = AggregateToStreamName(typeof(TAggregate), correlationId);
            _connection.DeleteStreamAsync(streamName, ExpectedVersion.Any, hard).Wait();
        }

        public override TResult GetById<TResult>(string correlationId)
        {
            var streamName = AggregateToStreamName(typeof(TResult), correlationId);
            var streamEvents = new List<ResolvedEvent>();
            StreamEventsSlice currentSlice;
            var nextSliceStart = StreamPosition.Start;
            do
            {
                currentSlice = _connection.ReadStreamEventsForwardAsync(streamName, nextSliceStart, 200, false).Result;
                if (currentSlice.Status == SliceReadStatus.StreamNotFound)
                    throw new AggregateNotFoundException("Could not found aggregate of type " + typeof(TResult) +
                                                         " and id " + correlationId);
                nextSliceStart = (int)currentSlice.NextEventNumber;
                streamEvents.AddRange(currentSlice.Events);
            } while (!currentSlice.IsEndOfStream);
            var deserializedEvents = streamEvents.Select(e =>
                DeserializeObject(e.Event.Data, e.Event.Metadata) as Event);
            return BuildAggregate<TResult>(deserializedEvents);
        }

        private EventData CreateEventData(object @event)
        {
            IDictionary<string, string> metadata;
            var originalEventType = @event.GetType().Name;
            if (((Event)@event).Metadata != null)
            {
                metadata = ((Event)@event).Metadata;
                if (!metadata.ContainsKey("$correlationId"))
                    throw new Exception("The event metadata must contains a $correlationId");
                if (!metadata.ContainsKey(EventClrTypeHeader))
                    metadata.Add(EventClrTypeHeader, @event.GetType().AssemblyQualifiedName);
                else
                    metadata[EventClrTypeHeader] = @event.GetType().AssemblyQualifiedName;
                // Remove the metadata from the event body
                var tmp = (IDictionary<string, object>)@event.ToDynamic();
                tmp.Remove("Metadata");
                @event = tmp;
            }
            else
                throw new Exception("The event must have a $correlationId present in its metadata");
            var eventDataHeaders = SerializeObject(metadata);
            byte[] data;
            if (_cryptoService != null && _keyReader != null)
            {
                if (string.IsNullOrWhiteSpace(metadata["encrypt"]))
                    throw new Exception("id not found in the encrypt metadata field");
                var encryptionKey = _keyReader.Get(metadata["encrypt"]);
                if (string.IsNullOrWhiteSpace(encryptionKey))
                    throw new Exception("key not found with the given id");
                data = SerializeAndEncryptObject(@event, encryptionKey);
            }
            else
                data = SerializeObject(@event);
            var eventData = new EventData(Guid.NewGuid(), originalEventType, true, data, eventDataHeaders);
            return eventData;
        }

        private byte[] SerializeAndEncryptObject(object obj, string key)
        {
            var jsonString = JsonConvert.SerializeObject(obj);
            var data = Encoding.UTF8.GetBytes(Convert.ToBase64String(_cryptoService.Encrypt(jsonString, key)));
            return data;
        }

        private static byte[] SerializeObject(object obj)
        {
            var jsonObj = JsonConvert.SerializeObject(obj);
            var data = Encoding.UTF8.GetBytes(jsonObj);
            return data;
        }

        public override IEnumerable<Event> Save<TAggregate>(TAggregate aggregate)
        {
            // Synchronous save operation
            var streamName = AggregateToStreamName(aggregate.GetType(), aggregate.AggregateId);
            var events = aggregate.UncommitedEvents().ToList();
            var originalVersion = CalculateExpectedVersion(aggregate, events);
            var expectedVersion = originalVersion == 0 ? ExpectedVersion.NoStream : originalVersion - 1;
            var eventData = events.Select(CreateEventData).ToArray();
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

        public override async Task<IEnumerable<Event>> SaveAsync<TAggregate>(TAggregate aggregate)
        {
            var streamName = AggregateToStreamName(aggregate.GetType(), aggregate.AggregateId);
            var events = aggregate.UncommitedEvents().ToList();
            var originalVersion = CalculateExpectedVersion(aggregate, events);
            var expectedVersion = originalVersion == 0 ? ExpectedVersion.NoStream : originalVersion - 1;
            var eventData = events.Select(CreateEventData).ToArray();
            await SaveAsyncInternal(streamName, expectedVersion, eventData);
            aggregate.ClearUncommitedEvents();
            return events;
        }

        private async Task<WriteResult> SaveAsyncInternal(string streamName, long expectedVersion, EventData[] eventData)
        {
            try
            {
                return await _connection.AppendToStreamAsync(streamName, expectedVersion, eventData);
            }
            catch (AggregateException)
            {
                // Try to save using ExpectedVersion.Any to swallow silently WrongExpectedVersion error
                return await _connection.AppendToStreamAsync(streamName, ExpectedVersion.Any, eventData);
            }
        }

        public static string EventClrTypeHeader = "EventClrTypeName";

        public static T DeserializeObject<T>(byte[] data)
        {
            return (T)(DeserializeObject(data, typeof(T).AssemblyQualifiedName));
        }

        public static object DeserializeObject(byte[] data, string typeName)
        {
            try
            {
                var jsonString = Encoding.UTF8.GetString(data);
                return JsonConvert.DeserializeObject(jsonString, Type.GetType(typeName));
            }
            catch (Exception)
            {
                return null;
            }
        }

        private object DeserializeObject(byte[] data, byte[] metadata)
        {
            try
            {
                var dict = DeserializeObject<Dictionary<string, string>>(metadata);
                if (!dict.ContainsKey("$correlationId"))
                    throw new Exception("The metadata must contains a $correlationId");
                var bodyString = Encoding.UTF8.GetString(data);
                if (dict.ContainsKey("encrypt"))
                {
                    if (string.IsNullOrWhiteSpace(dict["encrypt"]))
                        throw new Exception("id not found in the encrypt metadata field");
                    var encryptionKey = _keyReader.Get(dict["encrypt"]);
                    if (string.IsNullOrWhiteSpace(encryptionKey))
                        throw new Exception("key not found with the given id");
                    bodyString = _cryptoService.Decrypt(Convert.FromBase64String(bodyString), encryptionKey);
                }
                var o1 = JObject.Parse(bodyString);
                var o2 = JObject.Parse(JsonConvert.SerializeObject(new { metadata = dict }));
                o1.Merge(o2, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Union });
                return JsonConvert.DeserializeObject(o1.ToString(), Type.GetType(dict[EventClrTypeHeader]));
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}