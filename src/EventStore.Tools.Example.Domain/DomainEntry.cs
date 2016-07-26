using System;
using System.Collections.Generic;
using System.Linq;
using EventStore.Tools.Example.Contracts.Commands;
using EventStore.Tools.Example.Domain.CommandHandlers;
using EventStore.Tools.Infrastructure;

namespace EventStore.Tools.Example.Domain
{
    public class DomainEntry
    {
        private readonly CommandDispatcher _commandDispatcher;

        public DomainEntry(IDomainRepository domainRepository, IEnumerable<Action<ICommand>> preExecutionPipe = null, IEnumerable<Action<object>> postExecutionPipe = null)
        {
            preExecutionPipe = preExecutionPipe ?? Enumerable.Empty<Action<ICommand>>();
            postExecutionPipe = CreatePostExecutionPipe(postExecutionPipe);
            _commandDispatcher = CreateCommandDispatcher(domainRepository, preExecutionPipe, postExecutionPipe);
        }

        private CommandDispatcher CreateCommandDispatcher(IDomainRepository domainRepository, IEnumerable<Action<ICommand>> preExecutionPipe, IEnumerable<Action<object>> postExecutionPipe)
        {
            var commandDispatcher = new CommandDispatcher(domainRepository, preExecutionPipe, postExecutionPipe);

            var associateAccountCommandHandler = new AssociateAccountCommandHandler(domainRepository);
            commandDispatcher.RegisterHandler<CreateAssociateAccount>(associateAccountCommandHandler);
            commandDispatcher.RegisterHandler<RegisterIncome>(associateAccountCommandHandler);
            commandDispatcher.RegisterHandler<RegisterExpense>(associateAccountCommandHandler);

            return commandDispatcher;
        }

        public void ExecuteCommand<TCommand>(TCommand command) where TCommand : ICommand
        {
            _commandDispatcher.ExecuteCommand(command);
        }
        private IEnumerable<Action<object>> CreatePostExecutionPipe(IEnumerable<Action<object>> postExecutionPipe)
        {
            if (postExecutionPipe == null) yield break;
            foreach (var action in postExecutionPipe)
            {
                yield return action;
            }
        }
    }
}
