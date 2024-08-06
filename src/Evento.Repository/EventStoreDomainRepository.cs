using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Evento.Repository;

public class EventStoreDomainRepository : DomainRepositoryBase
{
    public readonly string Category;
    private readonly IEventStoreConnection _connection;
    private readonly IKeyReader? _keyReader;
    private readonly ICryptoService? _cryptoService;

    private Dictionary<string, Type>? _mapping = new();
        
    public static string EventClrTypeHeader = "EventClrTypeName";

    public EventStoreDomainRepository(
        string category, IEventStoreConnection connection, Dictionary<string, Type>? mapping = null)
    {
        Category = category;
        _connection = connection;
        _mapping = mapping;
    }

    public EventStoreDomainRepository(
        string category, IEventStoreConnection connection, IKeyReader keyReader, ICryptoService cryptoService)
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

    public override TResult GetById<TResult>(string id)
    {
        return GetByIdInternal<TResult>(id, null);
    }

    public override TResult GetById<TResult>(string id, int eventsToLoad)
    {
        return GetByIdInternal<TResult>(id, eventsToLoad);
    }
        
    private TResult GetByIdInternal<TResult>(string id, int? eventsToLoad) where TResult : IAggregate, new()
    {
        var streamName = AggregateToStreamName(typeof(TResult), id);
        var streamEvents = LoadEvents(streamName, eventsToLoad);

        var deserializedEvents = streamEvents.Select(e => DeserializeObject(e) as Event);

        return BuildAggregate<TResult>(deserializedEvents);
    }

    private List<ResolvedEvent> LoadEvents(string streamName, int? maxEventsToLoad)
    {
        const int batchSize = 200;
        var allEvents = new List<ResolvedEvent>();
        var nextSliceStart = StreamPosition.Start;
        var eventsLoaded = 0;

        while (true)
        {
            var eventsToFetch = CalculateBatchSize(maxEventsToLoad, eventsLoaded, batchSize);
            var currentSlice = _connection.ReadStreamEventsForwardAsync(streamName, nextSliceStart, eventsToFetch, false).Result;

            if (currentSlice.Status == SliceReadStatus.StreamNotFound)
                throw new AggregateNotFoundException($"Could not find aggregate with id {streamName}");

            allEvents.AddRange(currentSlice.Events);
            eventsLoaded += currentSlice.Events.Length;
            nextSliceStart = (int)currentSlice.NextEventNumber;

            if (IsEndOfStream(currentSlice, maxEventsToLoad, eventsLoaded))
                break;
        }

        return allEvents;
    }

    private static int CalculateBatchSize(int? total, int loaded, int batchSize)
    {
        return total.HasValue ? Math.Min(batchSize, total.Value - loaded) : batchSize;
    }

    private static bool IsEndOfStream(StreamEventsSlice currentSlice, int? maxEventsToLoad, int eventsLoaded)
    {
        return currentSlice.IsEndOfStream || eventsLoaded >= maxEventsToLoad;
    }

    public override void DeleteAggregate<TAggregate>(string correlationId, bool hard)
    {
        var streamName = AggregateToStreamName(typeof(TAggregate), correlationId);
        _connection.DeleteStreamAsync(streamName, ExpectedVersion.Any, hard).Wait();
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


    private static T? DeserializeObject<T>(byte[] data) where T : class
    {
        var aqn = typeof(T).AssemblyQualifiedName;
            
        if (aqn is null)
            return null;
            
        return (T?)DeserializeObject(data, aqn);
    }

    private static object? DeserializeObject(byte[] data, string typeName)
    {
        try
        {
            var jsonString = Encoding.UTF8.GetString(data);
            var t = Type.GetType(typeName);
            return t is not null 
                ? JsonConvert.DeserializeObject(jsonString, t)
                : throw new Exception("");
        }
        catch (Exception)
        {
            return null;
        }
    }

    private object? DeserializeObject(ResolvedEvent re)
    {
        try
        {
            var metadataDict = DeserializeObject<Dictionary<string, string>>(re.Event.Metadata);
            if (metadataDict is null || !metadataDict.ContainsKey("$correlationId"))
                throw new Exception("The metadata must contain a $correlationId");

            var bodyString = Encoding.UTF8.GetString(re.Event.Data);
            bodyString = DecryptIfNecessary(bodyString, metadataDict);

            var eventObject = JObject.Parse(bodyString);
            var metadataObject = JObject.Parse(JsonConvert.SerializeObject(new { metadata = metadataDict }));
            eventObject.Merge(metadataObject, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Union });

            var targetType = GetEventType(re.Event.EventType, metadataDict);
            if (targetType is null)
                throw new Exception(
                    $"Could not determine type to deserialize to: " +
                    $"EventType {re.Event.EventType} / Header {metadataDict[EventClrTypeHeader]}");
                
            return JsonConvert.DeserializeObject(eventObject.ToString(), targetType);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private string DecryptIfNecessary(string bodyString, Dictionary<string, string> metadataDict)
    {
        if (_keyReader is null || _cryptoService is null)
            throw new Exception("Cryptography services are null.");

        if (!metadataDict.TryGetValue("encrypt", out var encrypt)) 
            return bodyString;
            
        if (string.IsNullOrWhiteSpace(encrypt))
            throw new Exception("ID not found in the encrypt metadata field");

        var encryptionKey = _keyReader.Get(encrypt);
        if (string.IsNullOrWhiteSpace(encryptionKey))
            throw new Exception("Key not found with the given ID");

        return _cryptoService.Decrypt(Convert.FromBase64String(bodyString), encryptionKey);
    }

    private Type? GetEventType(string eventType, Dictionary<string, string> metadataDict)
    {
        if (_mapping == null || !_mapping.TryGetValue(eventType, out var targetType))
            targetType = Type.GetType(metadataDict[EventClrTypeHeader]);
            
        return targetType;
    }
}