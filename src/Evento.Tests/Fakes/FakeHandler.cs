using System.Threading.Tasks;

namespace Evento.Tests.Fakes
{
    internal class FakeHandler : 
        IHandle<CreateFakeCommand>,
        IHandleAsync<CreateFakeCommand>
    {
        public IAggregate Handle(CreateFakeCommand command)
        {
            return FakeAggregate.Create(command);
        }

        public async Task<IAggregate> HandleAsync(CreateFakeCommand command)
        {
            return await Task.Run(() => { 
                return FakeAggregate.Create(command); 
            });
        }
    }
}
