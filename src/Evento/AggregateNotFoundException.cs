using System;

namespace Evento
{
    public class AggregateNotFoundException : Exception
    {
        public AggregateNotFoundException(string message)
            : base(message)
        {
        }
    }
}