using System.Collections.Generic;

namespace Evento.Tests.Fakes
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
