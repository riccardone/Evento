using System;
using EventStore.Tools.Infrastructure;

namespace EventStore.Tools.Example.Contracts.Events
{
    public class AssociateAccountCreated : DomainEvent
    {
        public Guid AssociateId { get; }

        public AssociateAccountCreated(Guid associateAccountId, Guid associateId)
        {
            Id = associateAccountId.ToString();
            AssociateId = associateId;
        }

        public override bool Equals(object obj)
        {
            var item = obj as AssociateAccountCreated;
            return item != null && item.AssociateId.Equals(AssociateId) && item.Id.Equals(Id);
        }
    }
}
