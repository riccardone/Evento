using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Evento.Repository;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Newtonsoft.Json;

namespace Evento.TestClient
{
    class Program
    {
        private static IDomainRepositoryAsync _repository;
        private static string _inputStream = "testStream";

        static void Main(string[] args)
        {
            try
            {
                var port = 1113;
                if (args.Length > 0 && int.TryParse(args[0], out port))
                    Console.WriteLine($"Connecting to localhost on port {port}");
                var conn = EventStoreConnection.Create(ConnectionSettings.Create(), new Uri($"tcp://admin:changeit@localhost:{port}"));
                conn.ConnectAsync().Wait();
                TestConnection(conn);
                _repository = new EventStoreDomainRepositoryAsync("testclient", conn);
                CreatePersistentSubscription(conn);
                conn.ConnectToPersistentSubscriptionAsync(_inputStream, "TestGroup", EventAppeared, SubscriptionDropped);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Console.WriteLine("press enter to exit");
            Console.ReadLine();
        }

        private static void TestConnection(IEventStoreConnection conn)
        {
            conn.AppendToStreamAsync(_inputStream, ExpectedVersion.Any,
                new[] { CreateSample(1), CreateSample(2), CreateSample(3) }).Wait();
        }

        static EventData CreateSample(int i)
        {
            var sampleObject = new { a = i };
            var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(sampleObject));
            var metadata = Encoding.UTF8.GetBytes("{}");
            var eventPayload = new EventData(Guid.NewGuid(), "event-type", true, data, metadata);
            return eventPayload;
        }

        private static void CreatePersistentSubscription(IEventStoreConnection conn)
        {
            try
            {
                conn.CreatePersistentSubscriptionAsync(_inputStream, "TestGroup", PersistentSubscriptionSettings.Create().StartFromBeginning(),
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
            EventAppearedAsync(arg1, arg2).Wait();
        }

        private static async Task EventAppearedAsync(EventStorePersistentSubscriptionBase arg1, ResolvedEvent arg2)
        {
            IAggregate aggregate;
            try
            {
                aggregate = await _repository.GetByIdAsync<TestAggregate>(arg2.OriginalEvent.EventId.ToString()); // This will always fail
            }
            catch (AggregateNotFoundException)
            {
                aggregate = TestAggregate.Create(new CreateTest(arg2.OriginalEvent.EventId.ToString(),
                    new Dictionary<string, string> {{"$correlationId", Guid.NewGuid().ToString()}}));
            }

            await _repository.SaveAsync(aggregate);
            Console.WriteLine($"Message '{arg2.OriginalEvent.EventId}' handled");
            Console.WriteLine($"Aggregate '{aggregate.AggregateId}' created");
        }
    }
}
