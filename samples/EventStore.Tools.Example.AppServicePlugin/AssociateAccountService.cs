using EventStore.Tools.Example.Contracts.Commands;
using EventStore.Tools.Example.Domain.Aggregates;
using EventStore.Tools.Infrastructure;

namespace EventStore.Tools.Example.AppServicePlugin
{
    internal class AssociateAccountService : 
        IHandle<CreateAssociateAccount>, 
        IHandle<RegisterIncome>,
        IHandle<RegisterExpense>
    {
        private readonly IDomainRepository _domainRepository;

        public AssociateAccountService(IDomainRepository domainRepository)
        {
            _domainRepository = domainRepository;
        }

        public IAggregate Handle(CreateAssociateAccount command)
        {
            try
            {
                return _domainRepository.GetById<AssociateAccount>(command.AssociateAccountId.ToString());
            }
            catch (AggregateNotFoundException)
            {
                return AssociateAccount.Create(command.AssociateAccountId, command.AssociateId);
            }
        }

        public IAggregate Handle(RegisterIncome command)
        {
            var associateAccount = _domainRepository.GetById<AssociateAccount>(command.AssociateAccountId.ToString());
            associateAccount.RegisterIncome(command.Value, command.Description);
            return associateAccount;
        }

        public IAggregate Handle(RegisterExpense command)
        {
            var associateAccount = _domainRepository.GetById<AssociateAccount>(command.AssociateAccountId.ToString());
            associateAccount.RegisterExpense(command.Value, command.Description);
            return associateAccount;
        }
    }
}
