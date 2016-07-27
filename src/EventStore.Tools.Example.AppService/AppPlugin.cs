using System;
using System.Collections.Generic;
using EventStore.ClientAPI;
using EventStore.Tools.Infrastructure;

namespace EventStore.Tools.Example.AppService
{
    // TODO use MEF without reference plugin lib
    // look at this post
    // http://stackoverflow.com/a/13519514
    public class AppPlugin
    {
        private IEventStoreConnection _connection;
        private Position? _latestPosition;
        private MessageHandler _messageHandler;

        public void Stop() { }

        public bool Start(IDomainRepository domainRepository, IEventStoreConnection connection, IEnumerable<Action<ICommand>> preExecutionPipe = null, IEnumerable<Action<object>> postExecutionPipe = null)
        {
            _connection = connection;
            _messageHandler = new MessageHandler(domainRepository, preExecutionPipe, postExecutionPipe);
            _latestPosition = Position.Start;
            var settings = new CatchUpSubscriptionSettings(10, 100, false, true);
            _connection.SubscribeToAllFrom(_latestPosition, settings, HandleEvent);
            // TODO instantiate domainentry and subscribe all 
            return true;
        }

        private void HandleEvent(EventStoreCatchUpSubscription arg1, ResolvedEvent arg2)
        {
            var @event = SerializationUtils.DeserializeEvent(arg2.OriginalEvent);
            if (@event == null)
            {
                return;
            }
            var eventType = @event.GetType(); // debug help
            if (@event is IMessage)
            {
                if (@event is IEvent)
                    _messageHandler.Publish(@event as IEvent);
                if (@event is ICommand)
                    _messageHandler.Send(@event as ICommand);
            }
            _latestPosition = arg2.OriginalPosition;

            //if (@event == null || !_latestPosition.HasValue) return;

            //var eventType2 = @event.GetType();

            //if (_eventHandlerMapping.ContainsKey(eventType2))
            //    SavePosition(_latestPosition.Value);
        }
    }
}
