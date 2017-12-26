using System.Collections.Generic;
using Evento;

namespace Infrastructure.Tests.Fakes
{
    internal class FakeAggregate : AggregateBase
    {
        public override string AggregateId => _correlationId;
        private string _correlationId;

        private FakeAggregate()
        {
            RegisterTransition<FakeAggregateCreated>(Apply);
        }

        private FakeAggregate(string testString, IDictionary<string, string> metadata) : this()
        {
            RaiseEvent(new FakeAggregateCreated(testString, metadata));
        }

        private void Apply(FakeAggregateCreated obj)
        {
            _correlationId = obj.Metadata["$correlationId"];
        }

        public static FakeAggregate Create(CreateFakeCommand cmd)
        {
            return new FakeAggregate(cmd.TestString, cmd.Metadata);
        }
    }
}
