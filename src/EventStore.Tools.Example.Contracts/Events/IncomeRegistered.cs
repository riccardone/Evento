using System;
using EventStore.Tools.Infrastructure;

namespace EventStore.Tools.Example.Contracts.Events
{
    public class IncomeRegistered : DomainEvent
    {
        public decimal Value { get; }
        public string Description { get; }
        public decimal Balance { get; }

        public IncomeRegistered(Guid id, decimal value, string description, decimal balance)
        {
            Id = id.ToString();
            Value = value;
            Description = description;
            Balance = balance;
        }
    }
}
