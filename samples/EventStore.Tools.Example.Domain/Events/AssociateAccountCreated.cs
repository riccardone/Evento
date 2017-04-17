using System;
using EventStore.Tools.Infrastructure;

namespace EventStore.Tools.Example.Domain.Events
{
    public class AssociateAccountCreated : Event
    {
        public Guid CorrelationId { get; }
        public Guid AssociateId { get; }

        public AssociateAccountCreated(Guid correlationId, Guid associateId)
        {
            CorrelationId = correlationId;
            AssociateId = associateId;
        }
    }
}
