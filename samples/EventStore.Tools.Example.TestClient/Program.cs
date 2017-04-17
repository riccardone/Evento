using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using EventStore.Tools.Example.Messages.Commands;
using EventStore.Tools.Example.TestClient.ReadModel;
using EventStore.Tools.Infrastructure;
using Newtonsoft.Json;

namespace EventStore.Tools.Example.TestClient
{
    class Program
    {
        private static IEventStoreConnection _connection;
        
        static void Main(string[] args)
        {
            Connect();
            ReadModelSubscriber();
            var repeat = true;
            var correlationId = Guid.NewGuid();
            Console.WriteLine("Press 1 to send a Create command");
            Console.WriteLine("Press 2 to send a Register 10£ RegisterExpense command");
            Console.WriteLine("Press 3 to send a Register 5£ RegisterIncome command");
            Console.WriteLine("Press 5 to exit the program");
            do
            {
                var key = Convert.ToString(Console.ReadKey().KeyChar);
                int option;
                if (int.TryParse(key, out option))
                {
                    switch (option)
                    {
                        case 1:
                            SendCommand(new CreateAssociateAccount(correlationId, Guid.NewGuid()));
                            Console.WriteLine($"CreateAssociateAccount sent with correlationId {correlationId}");
                            break;
                        case 2:
                            SendCommand(new RegisterExpense(correlationId, 10, "test expense"));
                            Console.WriteLine($"RegisterExpense sent with correlationId {correlationId}");
                            break;
                        case 3:
                            SendCommand(new RegisterIncome(correlationId, 5, "test income"));
                            Console.WriteLine($"RegisterIncome sent with correlationId {correlationId}");
                            break;
                        case 5:
                            repeat = false;
                            break;
                        default:
                            Console.WriteLine("Unrecognised option");
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("Illegal input");
                }
            } while (repeat);
            _connection.Close();
            Console.WriteLine("Press Enter to exit");
            Console.ReadLine();
        }

        private static void SendCommand(Command command)
        {
            var eventData = new EventData(Guid.NewGuid(), command.GetType().Name, true, SerializeObject(command), null);
            _connection.AppendToStreamAsync("input-account", ExpectedVersion.Any, eventData).Wait();
        }

        private static void Connect()
        {
            var connSettings = ConnectionSettings.Create().SetDefaultUserCredentials(new UserCredentials("admin", "changeit"))
                .KeepReconnecting().KeepRetrying().Build();
            _connection = EventStoreConnection.Create(connSettings,
                new IPEndPoint(IPAddress.Loopback, 1113), "ES-TestClientSender");
            _connection.ConnectAsync().Wait();
        }

        private static byte[] SerializeObject(object obj)
        {
            var jsonObj = JsonConvert.SerializeObject(obj);
            var data = Encoding.UTF8.GetBytes(jsonObj);
            return data;
        }

        private static void ReadModelSubscriber()
        {
            _connection.SubscribeToAllFrom(Position.Start, CatchUpSubscriptionSettings.Default, EventAppeared);
        }

        private static void EventAppeared(EventStoreCatchUpSubscription eventStoreCatchUpSubscription, ResolvedEvent resolvedEvent)
        {
            if (resolvedEvent.OriginalEvent.EventType.StartsWith("$"))
                return;

            if (!resolvedEvent.Event.EventType.Equals("ExpenseRegistered") &&
                !resolvedEvent.Event.EventType.Equals("IncomeRegistered")) return;

            var evt =
                JsonConvert.DeserializeObject<CurrentBalanceDto>(Encoding.UTF8.GetString(resolvedEvent.Event.Data));

            var metadata =
                JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(
                    Encoding.UTF8.GetString(resolvedEvent.Event.Metadata));

            Console.WriteLine($"The account with correlatioId {metadata["$correlationId"]} has a current balance of £{evt.Balance}");
        }
    }
}
