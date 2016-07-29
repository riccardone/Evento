using System;
using System.Collections.Generic;
using System.Linq;

namespace EventStore.Tools.Infrastructure
{
    public class Bus : IBus
    {
        private readonly Dictionary<Type, Func<object, IAggregate>> _routes;
        private readonly IDomainRepository _domainRepository;
        private readonly IEnumerable<Action<object>> _postExecutionPipe;
        private readonly IEnumerable<Action<ICommand>> _preExecutionPipe;
        private readonly IEnumerable<Action<IEvent>> _preExecutionEventPipe;

        public Bus(IDomainRepository domainRepository, IEnumerable<Action<ICommand>> preExecutionPipe, IEnumerable<Action<object>> postExecutionPipe, IEnumerable<Action<IEvent>> preExecutionEventPipe = null)
        {
            _domainRepository = domainRepository;
            _postExecutionPipe = postExecutionPipe;
            _preExecutionPipe = preExecutionPipe ?? Enumerable.Empty<Action<ICommand>>();
            _preExecutionEventPipe = preExecutionEventPipe ?? Enumerable.Empty<Action<IEvent>>();
            _routes = new Dictionary<Type, Func<object, IAggregate>>();
        }

        public void RegisterCommandHandler<TCommand>(IHandle<TCommand> handler) where TCommand : class, ICommand
        {
            _routes.Add(typeof(TCommand), message => handler.Handle(message as TCommand));
        }

        public void RegisterEventHandler<TEvent>(IHandle<TEvent> handler) where TEvent : class, IEvent
        {
            _routes.Add(typeof(TEvent), message => handler.Handle(message as TEvent));
        }

        public void Send<TCommand>(TCommand command) where TCommand : ICommand
        {
            var commandType = command.GetType();
            if (!_routes.ContainsKey(commandType))
            {
                return;
                //throw new ApplicationException("Missing handler for " + commandType.Name);
            }
            RunPreExecutionPipe(command);
            var aggregate = _routes[commandType](command);
            var savedEvents = _domainRepository.Save(aggregate);
            RunPostExecutionPipe(savedEvents);
        }

        public void Publish<TEvent>(TEvent evt) where TEvent : IEvent
        {
            var commandType = evt.GetType();
            if (!_routes.ContainsKey(commandType))
            {
                return;
                //throw new ApplicationException("Missing handler for " + commandType.Name);
            }
            RunPreExecutionPipe(evt);
            var aggregate = _routes[commandType](evt);
            var savedEvents = _domainRepository.Save(aggregate);
            RunPostExecutionPipe(savedEvents);
        }

        private void RunPostExecutionPipe(IEnumerable<object> savedEvents)
        {
            foreach (var savedEvent in savedEvents)
            {
                foreach (var action in _postExecutionPipe)
                {
                    action(savedEvent);
                }
            }
        }

        private void RunPreExecutionPipe(ICommand command)
        {
            foreach (var action in _preExecutionPipe)
            {
                action(command);
            }
        }

        private void RunPreExecutionPipe(IEvent evt)
        {
            foreach (var action in _preExecutionEventPipe)
            {
                action(evt);
            }
        }
    }
}
