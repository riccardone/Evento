using System.Text;
using Evento.Repository.Grpc;
using KurrentDB.Client;
using Newtonsoft.Json;

namespace Evento.TestClient.Grpc
{
    class Program
    {
        private static IDomainRepository _repository;
        private const string InputStream = "testStream";
        private const string Category = "testclient";
        private const string AggregateName = $"testclient-{nameof(TestAggregate)}-{InputStream}";
        private const int ExpectedEvents = 1;

        static void Main(string[] args)
        {
            try
            {
                var port = 2113;
                if (args.Length > 0 && int.TryParse(args[0], out port))
                    Console.WriteLine($"Connecting to localhost on port {port}");
                var settings = KurrentDBClientSettings.Create($"esdb://admin:changeit@127.0.0.1:{port}?tls=false");
                settings.ConnectivitySettings.Insecure = true;
                var conn = new KurrentDBClient(settings);
                _repository = new EventStoreDomainRepository(Category, conn);
                var agg = CreateOrGetAggregate(conn);
                var events = conn.ReadStreamAsync(Direction.Forwards, AggregateName, StreamPosition.Start).CountAsync().Result;
                if(events < ExpectedEvents)
                {
                    throw new Exception("Test failed");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Console.WriteLine("press enter to exit");
            Console.ReadLine();
        }

        private static void AppendEvents(KurrentDBClient conn)
        {
            conn.AppendToStreamAsync(InputStream, StreamState.Any,
                new[] { CreateSample(1), CreateSample(2), CreateSample(3) }).Wait();
        }

        static EventData CreateSample(int i)
        {
            var sampleObject = new { a = i };
            var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(sampleObject));
            var metadata = Encoding.UTF8.GetBytes("{}");
            var eventPayload = new EventData(Uuid.FromGuid(Guid.NewGuid()), "event-type", data, metadata);
            return eventPayload;
        }

        private static IAggregate CreateOrGetAggregate(KurrentDBClient conn)
        {
            IAggregate aggregate = null;
            try
            {
                aggregate = _repository.GetById<TestAggregate>(InputStream);
            }
            catch (AggregateNotFoundException)
            {
                aggregate = TestAggregate.Create(new CreateTest(InputStream, new Dictionary<string, string>()
                {
                    { "$correlationId", Guid.NewGuid().ToString() }
                }));
                _repository.Save(aggregate);
            }
            return aggregate;
        }
    }
}
