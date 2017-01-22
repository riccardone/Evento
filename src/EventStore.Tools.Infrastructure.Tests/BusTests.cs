using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EventStore.Tools.Infrastructure.Tests
{
    [TestClass]
    public class BusTests
    {
        [TestMethod]
        public void CreateBus()
        {
            var bus = new Bus(new InMemoryDomainRespository(), null, null);
            bus.RegisterCommandHandler(new FakeHandler());
            Assert.IsTrue(bus.CanHandle(typeof(FakeCommand)));
        }

        class FakeAggregate : AggregateBase
        {
            public override string AggregateId { get; }

            public FakeAggregate(string id)
            {
                AggregateId = id;
            }
        }

        class FakeCommand : ICommand {}

        class FakeHandler : IHandle<FakeCommand>
        {
            public IAggregate Handle(FakeCommand command)
            {
                return new FakeAggregate(Guid.NewGuid().ToString());
            }
        }
    }
}
