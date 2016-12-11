using System;
using EventStore.Tools.Example.Contracts.Commands;
using EventStore.Tools.Example.Contracts.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EventStore.Tools.Example.Tests.AssociateAccountTests
{
    [TestClass]
    public class AssociateAccountTests : TestBase
    {
        [TestMethod]
        public void WhenCreateAssociateAccount_ThenIExpectNoErrors()
        {
            var associateAccountId = Guid.NewGuid();
            var associateId = Guid.NewGuid();

            When(new CreateAssociateAccount(associateAccountId, associateId));
            Then(new AssociateAccountCreated(associateAccountId, associateId));
        }
    }
}
