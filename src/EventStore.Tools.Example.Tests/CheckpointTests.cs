using EventStore.ClientAPI;
using EventStore.Tools.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EventStore.Tools.Example.Tests
{
    [TestClass]
    public class CheckpointTests
    {
        [TestMethod]
        [TestCategory("IntegrationTest")]
        public void SaveThePositionAndGetIt()
        {
            // Set up
            var repo = new EventStoreCheckpointRepository(Configuration.CreateConnection("test"), "CheckpointTest");
            var expectedPosition = new Position(20, 10);

            // Act
            repo.Save(expectedPosition);
            var savedPosition = repo.Get();

            // Verify
            Assert.IsTrue(savedPosition.Equals(expectedPosition));
        }
    }
}
