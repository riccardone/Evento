using System;
using EventStore.Tools.Infrastructure;

namespace EventStore.Tools.Example.Contracts.Commands
{
    public class RegisterExpense : ICommand
    {
        public Guid AssociateAccountId { get; }
        public decimal Value { get; }
        public string Description { get; }

        public RegisterExpense(Guid associateAccountId, decimal value, string description)
        {
            AssociateAccountId = associateAccountId;
            Value = value;
            Description = description;
        }
    }
}
