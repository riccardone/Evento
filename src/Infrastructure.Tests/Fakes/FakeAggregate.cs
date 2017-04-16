using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.Tools.Infrastructure;

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

        private FakeAggregate(string id, string testString) : this()
        {
            RaiseEvent(new FakeAggregateCreated(id, testString));
        }

        private void Apply(FakeAggregateCreated obj)
        {
            _correlationId = obj.Id;
        }

        public static FakeAggregate Create(CreateFakeCommand cmd)
        {
            return new FakeAggregate(cmd.Id, cmd.TestString);
        }
    }
}
