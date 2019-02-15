namespace Evento.TestClient
{
    public class TestAggregate : AggregateBase
    {
        public override string AggregateId => _id;
        private string _id;

        public TestAggregate()
        {
            RegisterTransition<TestCreated>(Apply);
        }

        public TestAggregate(TestCreated evt) : this()
        {
            RaiseEvent(evt);
        }

        private void Apply(TestCreated obj)
        {
            _id = obj.Id;
        }

        public static IAggregate Create(CreateTest cmd)
        {
            return new TestAggregate(new TestCreated(cmd.Id, cmd.Metadata));
        }
    }
}
