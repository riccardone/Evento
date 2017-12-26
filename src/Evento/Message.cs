using System.Collections.Generic;

namespace Evento
{
    public interface Message 
    {
        IDictionary<string, string> Metadata { get; }
    }
}
