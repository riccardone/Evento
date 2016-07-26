using EventStore.Tools.Example.Contracts.Commands;
using EventStore.Tools.Example.Domain.Aggregates;
using EventStore.Tools.Example.Domain.Exceptions;
using EventStore.Tools.Infrastructure;

namespace EventStore.Tools.Example.Domain.CommandHandlers
{
    internal class AssociateAccountCommandHandler : 
        IHandle<CreateAssociateAccount>, 
        IHandle<RegisterIncome>,
        IHandle<RegisterExpense>
    {
        private readonly IDomainRepository _domainRepository;

        public AssociateAccountCommandHandler(IDomainRepository domainRepository)
        {
            _domainRepository = domainRepository;
        }

        public IAggregate Handle(CreateAssociateAccount command)
        {
            try
            {
                _domainRepository.GetById<AssociateAccount>(command.AssociateAccountId.ToString());
                throw new AssociateAccountAlreadExistsException(command.AssociateAccountId);
            }
            catch (AggregateNotFoundException)
            {
                //Expect this
            }
            return AssociateAccount.Create(command.AssociateAccountId, command.AssociateId);
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
