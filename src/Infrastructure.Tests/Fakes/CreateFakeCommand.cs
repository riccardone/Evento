using EventStore.Tools.Infrastructure;

namespace Infrastructure.Tests.Fakes
{
    internal class CreateFakeCommand : Command
    {
        public string Id { get; }
        public string TestString { get; }

        public CreateFakeCommand(string id, string testString)
        {
            Id = id;
            TestString = testString;
        }
    }
}
