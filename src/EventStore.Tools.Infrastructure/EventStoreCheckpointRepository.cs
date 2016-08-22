using System;
using System.Collections.Generic;
using System.Text;
using EventStore.ClientAPI;
using Newtonsoft.Json;

namespace EventStore.Tools.Infrastructure
{
    public class EventStoreCheckpointRepository : ICheckpointRepository
    {
        public string StreamId { get; }
        private readonly IEventStoreConnection _connection;
        private Position _lastPosition = Position.Start;

        public EventStoreCheckpointRepository(IEventStoreConnection connection, string streamId)
        {
            StreamId = streamId;
            _connection = connection;
        }

        public Position Get()
        {
            var resolvedEvent = _connection.ReadEventAsync(StreamId, StreamPosition.End, false).Result.Event;
            if (resolvedEvent == null) return _lastPosition;
            var @event = SerializationUtils.DeserializeEvent(resolvedEvent.Value.Event);
            var changed = @event as PositionChanged;
            if (changed != null)
                _lastPosition = new Position(changed.Commit, changed.Prepare);
            return _lastPosition;
        }

        public void Save(Position position)
        {
            _connection.SetStreamMetadataAsync(StreamId, ExpectedVersion.Any, StreamMetadata.Build().SetMaxCount(1)).Wait();
            var evtId = Guid.NewGuid();
            var evt = new PositionChanged(evtId.ToString(), position.CommitPosition, position.PreparePosition);
            var dataToPost = CreateEventData(evt);
            _connection.AppendToStreamAsync(StreamId, ExpectedVersion.StreamExists, dataToPost).Wait();
        }

        private EventData CreateEventData(object @event)
        {
            var eventHeaders = new Dictionary<string, string>()
            {
                {
                    SerializationUtils.EventClrTypeHeader, @event.GetType().AssemblyQualifiedName
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
