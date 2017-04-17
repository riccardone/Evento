using System;
using System.Text;
using EventStore.ClientAPI;
using EventStore.Tools.Example.Messages.Commands;
using EventStore.Tools.Infrastructure;
using log4net;
using Newtonsoft.Json;

namespace EventStore.Tools.Example.AppService
{
    public class AssociateAccountEndPoint 
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AssociateAccountEndPoint));
        private readonly IDomainRepository _domainRepository;
        private readonly IEventStoreConnection _connection;
        private readonly AssociateAccountHandler _handler;

        public AssociateAccountEndPoint(IDomainRepository domainRepository, IEventStoreConnection connection)
        {
            _domainRepository = domainRepository;
            _connection = connection;
            _handler = new AssociateAccountHandler(domainRepository);
        }

        public bool Start()
        {
            _connection.SubscribeToAllAsync(false, EventAppeared).Wait();
            Log.Info("AssociateAccount EndPoint started");
            return true;
        }

        private void EventAppeared(EventStoreSubscription eventStoreSubscription, ResolvedEvent resolvedEvent)
        {
            if (resolvedEvent.OriginalEvent.EventType.StartsWith("$"))
                return;
            var json = Encoding.UTF8.GetString(resolvedEvent.OriginalEvent.Data);
            try
            {
                IAggregate aggregate;
                switch (resolvedEvent.OriginalEvent.EventType)
                {
                    case "CreateAssociateAccount":
                        aggregate = _handler.Handle(JsonConvert.DeserializeObject<CreateAssociateAccount>(json));
                        break;
                    case "RegisterExpense":
                        aggregate = _handler.Handle(JsonConvert.DeserializeObject<RegisterExpense>(json));
                        break;
                    case "RegisterIncome":
                        aggregate = _handler.Handle(JsonConvert.DeserializeObject<RegisterIncome>(json));
                        break;
                    default:
                        return;
                }
                _domainRepository.Save(aggregate, aggregate.AggregateId);
                Log.Info($"Handled '{resolvedEvent.OriginalEvent.EventType}' AggregateId: {aggregate.AggregateId}");
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }
        public void Stop()
        {
            _connection.Close();
        }
    }
}
