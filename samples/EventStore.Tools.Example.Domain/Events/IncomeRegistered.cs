using EventStore.Tools.Infrastructure;

namespace EventStore.Tools.Example.Domain.Events
{
    public class IncomeRegistered : Event
    {
        public decimal Value { get; }
        public string Description { get; }
        public decimal Balance { get; }

        public IncomeRegistered(decimal value, string description, decimal balance)
        {
            Value = value;
            Description = description;
            Balance = balance;
        }
    }
}
