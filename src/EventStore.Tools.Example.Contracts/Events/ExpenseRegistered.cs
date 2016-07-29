using System;
using EventStore.Tools.Infrastructure;

namespace EventStore.Tools.Example.Contracts.Events
{
    public class ExpenseRegistered : IEvent
    {
        public string Id { get; }
        public decimal Value { get; }
        public string Description { get; }
        public decimal Balance { get; }

        public ExpenseRegistered(Guid id, decimal value, string description, decimal balance)
        {
            Id = id.ToString();
            Value = value;
            Description = description;
            Balance = balance;
        }
    }
}
