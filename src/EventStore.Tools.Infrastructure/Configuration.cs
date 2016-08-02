using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

namespace EventStore.Tools.Infrastructure
{
    public static class Configuration
    {
        public static IEventStoreConnection CreateConnection()
        {
            return Connect(GetGossipEndPoints().ToArray());
        }

        private static IEventStoreConnection Connect(IPEndPoint[] endpoints)
        {
            IEventStoreConnection connection;
            if (endpoints.Length > 1)
            {
                connection = EventStoreConnection.Create(GetConnectionSettings(),
                    ClusterSettings.Create()
                        .DiscoverClusterViaGossipSeeds()
                        .SetGossipSeedEndPoints(endpoints));
            }
            else
            {
                var endPoint = GetTcpEndpoints().First();
                connection = EventStoreConnection.Create(GetConnectionSettings(), endPoint);
            }
            connection.ConnectAsync();
            return connection;
        }

        private static ConnectionSettings GetConnectionSettings()
        {
            return ConnectionSettings.Create()
                .UseConsoleLogger()
                .SetDefaultUserCredentials(GetUserCredentials())
                .WithConnectionTimeoutOf(TimeSpan.FromSeconds(5))
                .KeepRetrying()
                .KeepReconnecting();
        }

        private static UserCredentials GetUserCredentials()
        {
            return new UserCredentials(EventStoreUserName, EventStorePassword);
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

        public static List<IPEndPoint> GetGossipEndPoints()
        {
            var gossipEndpoints = new List<IPEndPoint>();
            foreach (var key in ConfigurationManager.AppSettings.AllKeys.Where(a => a.StartsWith("EventStoreNode")))
            {
                var nodeNumber = int.Parse(key.Substring("EventStoreNode".Length, 1));
                var httpPort = int.Parse(ConfigurationManager.AppSettings[$"EventStoreNode{nodeNumber}HttpPort"]);
                var hostAddress = Dns.GetHostAddresses(ConfigurationManager.AppSettings[$"EventStoreNode{nodeNumber}HostName"])[0];
                if (!gossipEndpoints.Exists(a => a.Port == httpPort && Equals(a.Address, hostAddress)))
                    gossipEndpoints.Add(new IPEndPoint(hostAddress, httpPort));
            }
            return gossipEndpoints;
        }

        private static List<IPEndPoint> GetTcpEndpoints()
        {
            var gossipEndpoints = new List<IPEndPoint>();
            foreach (var key in ConfigurationManager.AppSettings.AllKeys.Where(a => a.StartsWith("EventStoreNode")))
            {
                var nodeNumber = int.Parse(key.Substring("EventStoreNode".Length, 1));
                var httpPort = int.Parse(ConfigurationManager.AppSettings[$"EventStoreNode{nodeNumber}TcpPort"]);
                var hostAddress = Dns.GetHostAddresses(ConfigurationManager.AppSettings[$"EventStoreNode{nodeNumber}HostName"])[0];
                if (!gossipEndpoints.Exists(a => a.Port == httpPort && Equals(a.Address, hostAddress)))
                    gossipEndpoints.Add(new IPEndPoint(hostAddress, httpPort));
            }
            return gossipEndpoints;
        }
    }
}
