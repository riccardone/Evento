using System.Threading.Tasks;

namespace Evento
{
    /// <summary>
    /// An interface to compose command handling functions toghether
    /// </summary>
    public interface IHandleAsync<in TMessage> where TMessage : Command
    {
        /// <summary>
        /// This method process the specified command and return an aggregate with uncommitted events
        /// </summary>
        /// <param name="command">The Command to be handled</param>
        /// <returns>The Aggregate containing uncommitted events</returns>
        Task<IAggregate> HandleAsync(TMessage command);
    }
}
