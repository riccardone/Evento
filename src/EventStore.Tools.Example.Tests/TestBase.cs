using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EventStore.Tools.Example.Domain;
using EventStore.Tools.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EventStore.Tools.Example.Tests
{
    public class TestBase
    {
        private InMemoryDomainRespository _domainRepository;
        private DomainEntry _domainEntry;
        private Dictionary<string, IEnumerable<DomainEvent>> _preConditions = new Dictionary<string, IEnumerable<DomainEvent>>();

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
            _preConditions = new Dictionary<string, IEnumerable<DomainEvent>>();
        }

        protected void When(ICommand command)
        {
            var application = BuildApplication();
            application.ExecuteCommand(command);
        }

        protected void Then(params DomainEvent[] expectedEvents)
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

        protected void Given(params DomainEvent[] existingEvents)
        {
            _preConditions = existingEvents
                .GroupBy(y => y.Id)
                .ToDictionary(y => y.Key, y => y.AsEnumerable());
        }
    }
}
