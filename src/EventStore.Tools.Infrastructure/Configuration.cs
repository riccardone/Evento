using System.Configuration;
using System.Net;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

namespace EventStore.Tools.Infrastructure
{
    public static class Configuration
    {
        public static IEventStoreConnection CreateConnection()
        {
            return Connect();
        }

        private static IEventStoreConnection Connect()
        {
            ConnectionSettings settings =
                ConnectionSettings.Create()
                    .UseConsoleLogger()
                    .SetDefaultUserCredentials(new UserCredentials(EventStoreUserName, EventStorePassword))
                    .KeepReconnecting();
            var endPoint = new IPEndPoint(EventStoreIp, EventStoreTcpPort);
            var connection = EventStoreConnection.Create(settings, endPoint);
            //var connection = EventStoreConnection.Create(settings,
            //    ClusterSettings.Create()
            //        .DiscoverClusterViaGossipSeeds()
            //        .SetGossipSeedEndPoints(new IPEndPoint(EventStoreIp, 1114), new IPEndPoint(EventStoreIp, 2114),
            //            new IPEndPoint(EventStoreIp, 3114)));
            connection.ConnectAsync();
            return connection;
        }

        public static string EventStoreUserName
        {
            get
            {
                var eventStoreUserName = ConfigurationManager.AppSettings["EventStoreUserName"];
                return string.IsNullOrEmpty(eventStoreUserName) ? "admin" : eventStoreUserName;
            }
        }

        public static string EventStorePassword
        {
            get
            {
                var eventStorePassword = ConfigurationManager.AppSettings["EventStorePassword"];
                return string.IsNullOrEmpty(eventStorePassword) ? "changeit" : eventStorePassword;
            }
        }

        public static IPAddress EventStoreIp
        {
            get
            {
                var hostname = ConfigurationManager.AppSettings["EventStoreHostName"];
                if (string.IsNullOrEmpty(hostname))
                {
                    return IPAddress.Loopback;
                }
                var ipAddresses = Dns.GetHostAddresses(hostname);
                return ipAddresses[0];
            }
        }

        public static int EventStoreTcpPort
        {
            get
            {
                var esPort = ConfigurationManager.AppSettings["EventStoreTcpPort"];
                return string.IsNullOrEmpty(esPort) ? 1113 : int.Parse(esPort);
            }
        }

        public static int EventStoreHttpPort
        {
            get
            {
                var esPort = ConfigurationManager.AppSettings["EventStoreHttpPort"];
                return string.IsNullOrEmpty(esPort) ? 2113 : int.Parse(esPort);
            }
        }
    }
}
