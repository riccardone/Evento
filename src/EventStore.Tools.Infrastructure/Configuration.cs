using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Log;
using EventStore.ClientAPI.Projections;
using EventStore.ClientAPI.SystemData;

namespace EventStore.Tools.Infrastructure
{
    public static class Configuration
    {
        public static IEventStoreConnection CreateConnection(string name, ConnectionSettings connectionSettings = null, bool isOpen = true)
        {
            return Connect(GetGossipEndPoints().ToArray(), name, connectionSettings);
        }
        public static IEventStoreConnection CreateConnection(ConnectionSettings connectionSettings, bool isOpen = true)
        {
            return Connect(GetGossipEndPoints().ToArray(), null, connectionSettings);
        }
        public static IEventStoreConnection CreateConnection(bool isOpen = true) 
        {
            return Connect(GetGossipEndPoints().ToArray());
        }

        public static ProjectionsManager CreateProjectionManager()
        {
            var endPoints = GetGossipEndPoints().ToArray();
            var projectionsManager = new ProjectionsManager(new ConsoleLogger(),
                endPoints.Length > 1 ? endPoints.First() : GetDefaultHttpEndpoint(), TimeSpan.FromMinutes(1));
            return projectionsManager;
        }

        private static IEventStoreConnection Connect(IPEndPoint[] endpoints, string name = null, ConnectionSettings connectionSettings = null, bool isOpen = true)
        {
            IEventStoreConnection connection;
            if (endpoints.Length > 1)
            {
                connection = EventStoreConnection.Create(connectionSettings ?? GetConnectionSettings(),
                    ClusterSettings.Create()
                        .DiscoverClusterViaGossipSeeds()
                        .SetGossipSeedEndPoints(endpoints), name);
            }
            else if (endpoints.Length == 1)
            {
                var tcpPort = 1113;
                if (ConfigurationManager.AppSettings["EventStoreNode1TcpPort"] != null)
                    tcpPort = int.Parse(ConfigurationManager.AppSettings["EventStoreNode1TcpPort"]);
                connection = EventStoreConnection.Create(connectionSettings ?? GetConnectionSettings(), new IPEndPoint(endpoints.First().Address, tcpPort), name);
            }
            else
                connection = EventStoreConnection.Create(connectionSettings ?? GetConnectionSettings(), GetDefaultTcpEndpoint(), name);

            if (isOpen)
                connection.ConnectAsync().Wait();
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
        private static string EventStoreUserName
        {
            get
            {
                var eventStoreUserName = ConfigurationManager.AppSettings["EventStoreUserName"];
                return string.IsNullOrEmpty(eventStoreUserName) ? "admin" : eventStoreUserName;
            }
        }
        private static string EventStorePassword
        {
            get
            {
                var eventStorePassword = ConfigurationManager.AppSettings["EventStorePassword"];
                return string.IsNullOrEmpty(eventStorePassword) ? "changeit" : eventStorePassword;
            }
        }
        private static List<IPEndPoint> GetGossipEndPoints()
        {
            var gossipEndpoints = new List<IPEndPoint>();
            var nodeKeys = ConfigurationManager.AppSettings.AllKeys.Where(a => a.StartsWith("EventStoreNode"));
            var enumerable = nodeKeys as string[] ?? nodeKeys.ToArray();
            if (!enumerable.Any())
                return gossipEndpoints;
            foreach (var key in enumerable)
            {
                var nodeNumber = int.Parse(key.Substring("EventStoreNode".Length, 1));
                var httpPort = int.Parse(ConfigurationManager.AppSettings[$"EventStoreNode{nodeNumber}HttpPort"]);
                var hostAddress = Dns.GetHostAddresses(ConfigurationManager.AppSettings[$"EventStoreNode{nodeNumber}HostName"])[0];
                if (!gossipEndpoints.Exists(a => a.Port == httpPort && Equals(a.Address, hostAddress)))
                    gossipEndpoints.Add(new IPEndPoint(hostAddress, httpPort));
            }
            return gossipEndpoints;
        }
        private static IPEndPoint GetDefaultTcpEndpoint()
        {
            return new IPEndPoint(IPAddress.Loopback, 1113);
        }
        private static IPEndPoint GetDefaultHttpEndpoint()
        {
            return new IPEndPoint(IPAddress.Loopback, 2113);
        }
    }
}