using EventStore.ClientAPI;

namespace EventStore.Tools.Infrastructure
{
    public interface ICheckpointRepository
    {
        Position Get();
        void Save(Position position);
    }
}
