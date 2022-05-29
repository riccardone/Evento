using System.Text;
using Evento.Repository.Grpc;
using EventStore.Client;
using Newtonsoft.Json;

namespace Evento.TestClient.Grpc
{
    class Program
    {
        private static IDomainRepositoryAsync _repository;
        private const string InputStream = "testStream";

        static void Main(string[] args)
        {
            try
            {
                var port = 1113;
                if (args.Length > 0 && int.TryParse(args[0], out port))
                    Console.WriteLine($"Connecting to localhost on port {port}");
                var settings = EventStoreClientSettings.Create($"tcp://admin:changeit@127.0.0.1:{port}");
                var conn = new EventStoreClient(settings);
                TestConnection(conn);
                _repository = new EventStoreDomainRepositoryAsync("testclient", conn);
                // TODO
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Console.WriteLine("press enter to exit");
            Console.ReadLine();
        }

        private static void TestConnection(EventStoreClient conn)
        {
            conn.AppendToStreamAsync(InputStream, StreamState.Any,
                new[] { CreateSample(1), CreateSample(2), CreateSample(3) }).Wait();
        }

        static EventData CreateSample(int i)
        {
            var sampleObject = new { a = i };
            var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(sampleObject));
            var metadata = Encoding.UTF8.GetBytes("{}");
            var eventPayload = new EventData(new Uuid(), "event-type", data, metadata);
            return eventPayload;
        }

        private static void CreatePersistentSubscription(EventStoreClient conn)
        {
            throw new NotImplementedException();
        }

        private static async Task EventAppearedAsync(ResolvedEvent arg2)
        {
            IAggregate aggregate;
            try
            {
                aggregate = await _repository.GetByIdAsync<TestAggregate>(arg2.OriginalEvent.EventId.ToString()); // This will always fail
            }
            catch (AggregateNotFoundException)
            {
                aggregate = TestAggregate.Create(new CreateTest(arg2.OriginalEvent.EventId.ToString(),
                    new Dictionary<string, string> { { "$correlationId", Guid.NewGuid().ToString() } }));
            }

            await _repository.SaveAsync(aggregate);
            Console.WriteLine($"Message '{arg2.OriginalEvent.EventId}' handled");
            Console.WriteLine($"Aggregate '{aggregate.AggregateId}' created");
        }
    }
}
