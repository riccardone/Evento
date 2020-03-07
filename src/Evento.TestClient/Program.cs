using System;
using System.Collections.Generic;
using Evento.Repository;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

namespace Evento.TestClient
{
    class Program
    {
        private static IDomainRepository _repository;

        static void Main(string[] args)
        {
            var port = 1113;
            if (args.Length > 0 && int.TryParse(args[0], out port))
                Console.WriteLine($"Connecting to localhost on port {port}");
            var conn = EventStoreConnection.Create(GetConnectionBuilder(), new Uri($"tcp://localhost:{port}"));
            _repository = new EventStoreDomainRepository("testclient", conn);
            try
            {
                conn.ConnectAsync().Wait();
                CreatePersistentSubscription(conn);
                conn.ConnectToPersistentSubscription("domain-TestStream", "TestGroup",
                    (Action<EventStorePersistentSubscriptionBase, ResolvedEvent>)EventAppeared, SubscriptionDropped);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Console.WriteLine("press enter to exit");
            Console.ReadLine();
        }

        private static void CreatePersistentSubscription(IEventStoreConnection conn)
        {
            try
            {
                conn.CreatePersistentSubscriptionAsync("domain-TestStream", "TestGroup", PersistentSubscriptionSettings.Create().StartFromBeginning(),
                    new UserCredentials("admin", "changeit")).Wait();
            }
            catch (Exception)
            {
                // Already exist
            }
        }

        private static void SubscriptionDropped(EventStorePersistentSubscriptionBase arg1, SubscriptionDropReason arg2, Exception arg3)
        {
            Console.WriteLine(arg2);
            Console.WriteLine(arg3.GetBaseException().Message);
        }

        private static void EventAppeared(EventStorePersistentSubscriptionBase arg1, ResolvedEvent arg2)
        {
            IAggregate aggregate;
            try
            {
                aggregate = _repository.GetById<TestAggregate>(arg2.OriginalEvent.EventId.ToString()); // This will always fail
            }
            catch (AggregateNotFoundException)
            {
                aggregate = TestAggregate.Create(new CreateTest(arg2.OriginalEvent.EventId.ToString(),
                    new Dictionary<string, string> {{"$correlationId", Guid.NewGuid().ToString()}}));
            }

            _repository.SaveAsync(aggregate);
            Console.WriteLine($"Message '{arg2.OriginalEvent.EventId}' handled");
            Console.WriteLine($"Aggregate '{aggregate.AggregateId}' created");
        }

        private static ConnectionSettings GetConnectionBuilder()
        {
            var settings = ConnectionSettings.Create()
                .KeepRetrying()
                .KeepReconnecting();
            return settings;
        }
    }
}
