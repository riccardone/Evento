using System;
using System.Collections.Generic;
using System.Linq;
using EventStore.Tools.Example.Contracts.Events;
using EventStore.Tools.Example.Domain.Aggregates.ValueObjects;
using EventStore.Tools.Infrastructure;

namespace EventStore.Tools.Example.Domain.Aggregates
{
    public class AssociateAccount : AggregateBase
    {
        public override string AggregateId => Id.ToString();
        public Guid Id { get; private set; }
        public Guid AssociateId { get; private set; }
        public List<Income> Incomes { get; }
        public List<Expense> Expenses { get; }
        public decimal Balance { get; private set; }

        public AssociateAccount(Guid id, Guid associateId) : this()
        {
            RaiseEvent(new AssociateAccountCreated(id, associateId));
        }

        public AssociateAccount()
        {
            Incomes = new List<Income>();
            Expenses = new List<Expense>();
            RegisterTransition<AssociateAccountCreated>(Apply);
            RegisterTransition<IncomeRegistered>(Apply);
            RegisterTransition<ExpenseRegistered>(Apply);
        }

        private void Apply(ExpenseRegistered evt)
        {
            Expenses.Add(new Expense(evt.Value, evt.Description));
            Balance = evt.Balance;
        }

        private void Apply(IncomeRegistered evt)
        {
            Incomes.Add(new Income(evt.Value, evt.Description));
            Balance = evt.Balance;
        }

        private void Apply(AssociateAccountCreated evt)
        {
            Id = new Guid(evt.Id);
            AssociateId = evt.AssociateId;
        }

        internal static IAggregate Create(Guid id, Guid associateId)
        {
            return new AssociateAccount(id, associateId);
        }

        internal void RegisterIncome(decimal value, string description)
        {
            var incomesTotal = Incomes.Select(a => a.Value).Sum() + value;
            var currentBalance = incomesTotal - Expenses.Select(a => a.Value).Sum();

            RaiseEvent(new IncomeRegistered(Id, value, description, currentBalance));
        }

        internal void RegisterExpense(decimal value, string description)
        {
            var expensesTotal = Expenses.Select(a => a.Value).Sum() + value;
            var currentBalance = Incomes.Select(a => a.Value).Sum() - expensesTotal;

            RaiseEvent(new ExpenseRegistered(Id, value, description, currentBalance));
        }
    }
}
