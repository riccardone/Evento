using EventStore.Tools.Example.AppServicePlugin;
using EventStore.Tools.Infrastructure;
using Topshelf;

namespace EventStore.Tools.Example.Host
{
    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                var conn = Configuration.CreateConnection();
                var repo = new EventStoreDomainRepository("Example", conn);
                x.Service<AppServiceStrategy>(s =>
                {
                    s.ConstructUsing(name => new AppServiceStrategy());
                    s.WhenStarted(
                        (tc, hostControl) =>
                            tc.Start(repo, conn));
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();
                x.StartAutomatically();
                x.SetDescription("This process host any Application Service module");
                x.SetDisplayName("EventStore Host");
                x.SetServiceName("EventStore.Host");
            });
        }
    }
}
