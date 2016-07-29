using EventStore.Tools.PluginModel;

namespace EventStore.Tools.Example.AppServicePlugin
{
    public class AppServiceStrategyFactory : IServiceStrategyFactory
    {
        public IServiceStrategy Create()
        {
            return new AppServiceStrategy();
        }

        public string StrategyName => typeof(AppServiceStrategyFactory).Name;
    }
}
