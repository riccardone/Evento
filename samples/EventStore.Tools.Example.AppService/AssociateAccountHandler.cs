using EventStore.Tools.Example.Domain.Aggregates;
using EventStore.Tools.Example.Messages.Commands;
using EventStore.Tools.Infrastructure;

namespace EventStore.Tools.Example.AppService
{
    public class AssociateAccountHandler : 
        IHandle<CreateAssociateAccount>, 
        IHandle<RegisterIncome>,
        IHandle<RegisterExpense>
    {
        private readonly IDomainRepository _domainRepository;

        public AssociateAccountHandler(IDomainRepository domainRepository)
        {
            _domainRepository = domainRepository;
        }

        public IAggregate Handle(CreateAssociateAccount command)
        {
            try
            {
                return _domainRepository.GetById<AssociateAccount>(command.CorrelationId.ToString());
            }
            catch (AggregateNotFoundException)
            {
                return AssociateAccount.Create(command.CorrelationId, command.AssociateId);
            }
        }

        public IAggregate Handle(RegisterIncome command)
        {
            var associateAccount = _domainRepository.GetById<AssociateAccount>(command.CorrelationId.ToString());
            associateAccount.RegisterIncome(command.Value, command.Description);
            return associateAccount;
        }

        public IAggregate Handle(RegisterExpense command)
        {
            var associateAccount = _domainRepository.GetById<AssociateAccount>(command.CorrelationId.ToString());
            associateAccount.RegisterExpense(command.Value, command.Description);
            return associateAccount;
        }
    }
}
