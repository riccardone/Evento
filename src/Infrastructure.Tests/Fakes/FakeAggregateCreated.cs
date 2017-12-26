using System.Collections.Generic;
using Evento;

namespace Infrastructure.Tests.Fakes
{
    internal class FakeAggregateCreated : Event
    {
        public string TestString { get; }
        public IDictionary<string, string> Metadata { get; }
        public FakeAggregateCreated(string test, IDictionary<string, string> metadata)
        {
            TestString = test;
            Metadata = metadata;
        }
    }
}
