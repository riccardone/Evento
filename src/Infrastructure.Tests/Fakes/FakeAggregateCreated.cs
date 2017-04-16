using EventStore.Tools.Infrastructure;

namespace Infrastructure.Tests.Fakes
{
    internal class FakeAggregateCreated : Event
    {
        public string Id { get; }
        public string TestString { get; }

        public FakeAggregateCreated(string id, string test)
        {
            Id = id;
            TestString = test;
        }
    }
}
