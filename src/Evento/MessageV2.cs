using System.Collections.Generic;

namespace Evento
{
    public interface MessageV2 : Message
    {
        IDictionary<string, string> Metadata { get; }
    }
}
