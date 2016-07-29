using System;
using System.Collections.Generic;
using System.Linq;
using EventStore.Tools.Example.AppServicePlugin.CommandHandlers;
using EventStore.Tools.Example.Contracts.Commands;
using EventStore.Tools.Infrastructure;

namespace EventStore.Tools.Example.AppServicePlugin
{
    public class MessageHandler
    {
        private readonly Bus _bus;

        public MessageHandler(IDomainRepository domainRepository, IEnumerable<Action<ICommand>> preExecutionPipe = null, IEnumerable<Action<object>> postExecutionPipe = null)
        {
            preExecutionPipe = preExecutionPipe ?? Enumerable.Empty<Action<ICommand>>();
            postExecutionPipe = CreatePostExecutionPipe(postExecutionPipe);
            _bus = CreateBus(domainRepository, preExecutionPipe, postExecutionPipe);
        }

        private Bus CreateBus(IDomainRepository domainRepository, IEnumerable<Action<ICommand>> preExecutionPipe, IEnumerable<Action<object>> postExecutionPipe)
        {
            var bus = new Bus(domainRepository, preExecutionPipe, postExecutionPipe);

            var associateAccountCommandHandler = new AssociateAccountHandler(domainRepository);
            bus.RegisterCommandHandler<CreateAssociateAccount>(associateAccountCommandHandler);
            bus.RegisterCommandHandler<RegisterIncome>(associateAccountCommandHandler);
            bus.RegisterCommandHandler<RegisterExpense>(associateAccountCommandHandler);

            return bus;
        }

        public void Send<TCommand>(TCommand command) where TCommand : ICommand
        {
            _bus.Send(command);
        }
        public void Publish<TEvent>(TEvent evt) where TEvent : IEvent
        {
            _bus.Publish(evt);
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
