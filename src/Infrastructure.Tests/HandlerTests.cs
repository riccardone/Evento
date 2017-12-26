using System.Collections.Generic;
using System.Linq;
using Infrastructure.Tests.Fakes;
using NUnit.Framework;

namespace Infrastructure.Tests
{
    [TestFixture]
    public class HandlerTests
    {
        [Test]
        public void When_I_receive_a_command_I_should_raise_created_event()
        {
            // Assign
            const string correlationId = "correlationidexample-123";
            const string testString = "test";
            var cmd = new CreateFakeCommand(testString,
                new Dictionary<string, string> {{"$correlationId", correlationId}});

            // Act
            var results = new FakeHandler().Handle(cmd);

            // Assert
            Assert.IsTrue(((FakeAggregateCreated) results.UncommitedEvents().First()).TestString.Equals(testString));
        }
    }
}
