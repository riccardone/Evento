using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EventStore.Tools.Infrastructure
{
    public interface IBus
    {
        void Send<TCommand>(TCommand command) where TCommand : ICommand;
        void Publish<TEvent>(TEvent evt) where TEvent : IEvent;
        void RegisterHandler<TMessage>(IHandle<TMessage> handler) where TMessage : class, IMessage;
    }
}
