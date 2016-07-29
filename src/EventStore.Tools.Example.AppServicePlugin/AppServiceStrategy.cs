using System;
using System.Collections.Generic;
using EventStore.ClientAPI;
using EventStore.Tools.Infrastructure;
using EventStore.Tools.PluginModel;

namespace EventStore.Tools.Example.AppServicePlugin
{
    public class AppServiceStrategy : IServiceStrategy
    {
        private IEventStoreConnection _connection;
        private Position? _latestPosition;
        private DomainEntry _domainEntry;

        public void Stop() { }

        public bool Start(IDomainRepository domainRepository, IEventStoreConnection connection, IEnumerable<Action<ICommand>> preExecutionPipe = null, IEnumerable<Action<object>> postExecutionPipe = null)
        {
            _connection = connection;
            _domainEntry = new DomainEntry(domainRepository, preExecutionPipe, postExecutionPipe);
            _latestPosition = Position.Start;
            var settings = new CatchUpSubscriptionSettings(10, 100, false, true);
            _connection.SubscribeToAllFrom(_latestPosition, settings, HandleEvent);
            Console.WriteLine("AppServiceStrategy started");
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
                    _domainEntry.Publish(@event as IEvent);
                if (@event is ICommand)
                    _domainEntry.Send(@event as ICommand);
            }
            _latestPosition = arg2.OriginalPosition;

            // TODO implement save position feature
            //if (@event == null || !_latestPosition.HasValue) return;

            //var eventType2 = @event.GetType();

            //if (_eventHandlerMapping.ContainsKey(eventType2))
            //    SavePosition(_latestPosition.Value);
        }

        public bool Start()
        {
            var connection = Configuration.CreateConnection();
            var repo = new EventStoreDomainRepository("Example", connection);
            Start(repo, connection);
            return true;
        }
    }
}
