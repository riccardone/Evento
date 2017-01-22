using System;
using EventStore.Tools.Infrastructure;
using NUnit.Framework;

namespace Infrastructure.Tests
{
    [TestFixture]
    public class BusTests
    {
        [Test]
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
