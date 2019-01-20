using System.Collections.Generic;
using System.Linq;
using Evento.Repository;
using Evento.Tests.Fakes;
using EventStore.ClientAPI;
using Moq;
using NUnit.Framework;

namespace Evento.Tests
{
    [TestFixture]
    public class DomainRepositoryTests
    {
        [Test]
        public void When_I_save_an_aggregate_I_should_receive_the_saved_events()
        {
            // Assign
            const string correlationId = "correlationidexample-123";
            const string testString = "test";
            var cmd = new CreateFakeCommand(testString, new Dictionary<string, string>{{"$correlationId", correlationId}});
            var mockConnection = new Mock<IEventStoreConnection>();
            var repository = new EventStoreDomainRepository("domain", mockConnection.Object);

            // Act
            var results = repository.Save(new FakeHandler().Handle(cmd));

            // Assert
            Assert.IsTrue(((FakeAggregateCreated)results.Single()).Metadata["$correlationId"].Equals(correlationId));
        }
    }
}
