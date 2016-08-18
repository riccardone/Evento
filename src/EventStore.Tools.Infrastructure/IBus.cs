using System;

namespace EventStore.Tools.Infrastructure
{
    public interface IBus
    {
        void Send<TCommand>(TCommand command) where TCommand : ICommand;
        void Publish<TEvent>(TEvent evt) where TEvent : IEvent;
        void RegisterCommandHandler<TCommand>(IHandle<TCommand> handler) where TCommand : class, ICommand;
        void RegisterEventHandler<TEvent>(IHandle<TEvent> handler) where TEvent : class, IEvent;
        bool CanHandle(Type t);
    }
}
