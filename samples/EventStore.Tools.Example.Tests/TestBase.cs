using System;
using System.Collections.Generic;
using System.Linq;
using EventStore.Tools.Example.AppServicePlugin;
using EventStore.Tools.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EventStore.Tools.Example.Tests
{
    [TestClass]
    public class TestBase
    {
        private InMemoryDomainRespository _domainRepository;
        private DomainEntry _domainEntry;
        private Dictionary<string, IEnumerable<IEvent>> _preConditions = new Dictionary<string, IEnumerable<IEvent>>();

        private DomainEntry BuildApplication()
        {
            _domainRepository = new InMemoryDomainRespository();
            _domainRepository.AddEvents(_preConditions);
            return new DomainEntry(_domainRepository);
        }

        [ClassCleanup]
        public void TearDown()
        {
            IdGenerator.GenerateGuid = null;
            _preConditions = new Dictionary<string, IEnumerable<IEvent>>();
        }

        protected void When(ICommand command)
        {
            var application = BuildApplication();
            application.Send(command);
        }

        protected void Then(params IEvent[] expectedEvents)
        {
            var latestEvents = _domainRepository.GetLatestEvents().ToList();
            var expectedEventsList = expectedEvents.ToList();
            Assert.AreEqual(expectedEventsList.Count, latestEvents.Count);

            for (int i = 0; i < latestEvents.Count; i++)
            {
                Assert.AreEqual(expectedEvents[i], latestEvents[i]);
            }
        }

        protected void WhenThrows<TException>(ICommand command) where TException : Exception
        {
            try
            {
                When(command);
                Assert.Fail("Expected exception " + typeof(TException));
            }
            catch (TException)
            {
            }
        }

        protected void Given(params IEvent[] existingEvents)
        {
            _preConditions = existingEvents
                .GroupBy(y => y.Id)
                .ToDictionary(y => y.Key, y => y.AsEnumerable());
        }
    }
}
