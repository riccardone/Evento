using System;
using System.Collections.Generic;
using System.Linq;
using EventStore.Tools.Example.Domain.Aggregates.ValueObjects;
using EventStore.Tools.Example.Domain.Events;
using EventStore.Tools.Infrastructure;

namespace EventStore.Tools.Example.Domain.Aggregates
{
    public class AssociateAccount : AggregateBase
    {
        public override string AggregateId => CorrelationId.ToString();
        public Guid CorrelationId { get; private set; }
        public Guid AssociateId { get; private set; }
        public List<Income> Incomes { get; }
        public List<Expense> Expenses { get; }
        public decimal Balance { get; private set; }

        public AssociateAccount(Guid correlationId, Guid associateId) : this()
        {
            RaiseEvent(new AssociateAccountCreated(correlationId, associateId));
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
            CorrelationId = evt.CorrelationId;
            AssociateId = evt.AssociateId;
        }

        public static IAggregate Create(Guid correlationId, Guid associateId)
        {
            return new AssociateAccount(correlationId, associateId);
        }

        public void RegisterIncome(decimal value, string description)
        {
            var incomesTotal = Incomes.Select(a => a.Value).Sum() + value;
            var currentBalance = incomesTotal - Expenses.Select(a => a.Value).Sum();

            RaiseEvent(new IncomeRegistered(value, description, currentBalance));
        }

        public void RegisterExpense(decimal value, string description)
        {
            var expensesTotal = Expenses.Select(a => a.Value).Sum() + value;
            var currentBalance = Incomes.Select(a => a.Value).Sum() - expensesTotal;

            RaiseEvent(new ExpenseRegistered(value, description, currentBalance));
        }
    }
}
