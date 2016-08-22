namespace EventStore.Tools.Infrastructure
{
    public class PositionChanged : IEvent
    {
        public string Id { get; }
        public long Commit { get; }
        public long Prepare { get; }

        public PositionChanged(string id, long commit, long prepare)
        {
            Id = id;
            Commit = commit;
            Prepare = prepare;
        }
    }
}
