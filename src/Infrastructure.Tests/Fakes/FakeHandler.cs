using Evento;

namespace Infrastructure.Tests.Fakes
{
    internal class FakeHandler : IHandle<CreateFakeCommand>
    {
        public IAggregate Handle(CreateFakeCommand command)
        {
            return FakeAggregate.Create(command);
        }
    }
}
