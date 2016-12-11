using System;
using EventStore.Tools.Infrastructure;

namespace EventStore.Tools.Example.Contracts.Commands
{
    public class CreateAssociateAccount : ICommand
    {
        public Guid AssociateAccountId { get; }
        public Guid AssociateId { get; }

        public CreateAssociateAccount(Guid associateAccountId, Guid associateId)
        {
            AssociateAccountId = associateAccountId;
            AssociateId = associateId;
        }
    }
}
