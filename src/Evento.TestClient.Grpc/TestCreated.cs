namespace Evento.TestClient.Grpc
{
    public class TestCreated : Event
    {
        public string Id { get; }
        public IDictionary<string, string> Metadata { get; }

        public TestCreated(string id, IDictionary<string, string> metadata)
        {
            Id = id;
            Metadata = metadata;
        }
    }
}
