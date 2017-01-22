using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xunit;

namespace EventStore.Tools.Infrastructure.Tests
{
    public class BusTests
    {
        [Fact]
        public void CreateBus()
        {
            var bus = new Bus(new InMemoryDomainRespository(), null, null);
            bus.RegisterCommandHandler(new FakeHandler());
            Assert.True(bus.CanHandle(typeof(FakeCommand)));
        }

        class FakeAggregate : AggregateBase
        {
            public override string AggregateId { get; }

            public FakeAggregate(string id)
            {
                AggregateId = id;
            }
        }

        class FakeCommand : ICommand { }

        class FakeHandler : IHandle<FakeCommand>
        {
            public IAggregate Handle(FakeCommand command)
            {
                return new FakeAggregate(Guid.NewGuid().ToString());
            }
        }
    }
}
