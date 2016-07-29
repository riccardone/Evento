using EventStore.Tools.PluginModel;

namespace EventStore.Tools.Example.AppServicePlugin
{
    public class AppStrategyFactory : IServiceStrategyFactory
    {
        public IServiceStrategy Create()
        {
            return new AppPlugin();
        }

        public string StrategyName => typeof(AppStrategyFactory).Name;
    }
}
