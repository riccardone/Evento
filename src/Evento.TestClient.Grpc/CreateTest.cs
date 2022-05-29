﻿namespace Evento.TestClient.Grpc
{
    public class CreateTest : Command
    {
        public string Id { get; }
        public IDictionary<string, string> Metadata { get; }

        public CreateTest(string id, IDictionary<string, string> metadata)
        {
            Id = id;
            Metadata = metadata;
        }
    }
}
