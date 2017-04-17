using System.Net;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using EventStore.Tools.Example.AppService;
using EventStore.Tools.Infrastructure.Repository;
using log4net.Config;
using Topshelf;

namespace EventStore.Tools.Example.Host
{
    class Program
    {
        static void Main(string[] args)
        {
            XmlConfigurator.Configure();
            HostFactory.Run(x =>
            {
                x.UseLog4Net();
                x.Service<AssociateAccountEndPoint>(s =>
                {
                    var connSettings = ConnectionSettings.Create().SetDefaultUserCredentials(new UserCredentials("admin", "changeit"))
                        .KeepReconnecting().KeepRetrying().Build();
                    var endpointConnection = EventStoreConnection.Create(connSettings,
                        new IPEndPoint(IPAddress.Loopback, 1113), "ES-Subscriber");
                    var domainConnection = EventStoreConnection.Create(connSettings,
                        new IPEndPoint(IPAddress.Loopback, 1113), "ES-Processor");
                    endpointConnection.ConnectAsync().Wait();
                    domainConnection.ConnectAsync().Wait();
                    s.ConstructUsing(
                        name =>
                            new AssociateAccountEndPoint(new EventStoreDomainRepository("account", domainConnection),
                                endpointConnection));
                    s.WhenStarted((tc, hostControl) => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();
                x.UseAssemblyInfoForServiceInfo();
                x.SetDescription("Sample application");
                x.SetDisplayName("EventStore Example Host");
                x.SetServiceName("EventStore.Tools.Example.Host");
            });
        }
    }
}
