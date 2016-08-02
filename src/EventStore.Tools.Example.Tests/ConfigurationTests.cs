using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EventStore.Tools.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EventStore.Tools.Example.Tests
{
    [TestClass]
    public class ConfigurationTests
    {
        [TestMethod]
        public void TestCreateConnectionWithDefaultEndpoint()
        {
            // Set up

            // Act
            var res = Configuration.CreateConnection();

            // Verify
            Assert.IsNotNull(res);
        }
    }
}
