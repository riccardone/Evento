# EventStore.Tools.Infrastructure
This C# library contains an EventStore DomainRepository and a message Bus that can help to build event sourced components interacting with GetEventStore https://github.com/EventStore  
  
You can reference this project building the source code or using Nuget  
```
PM> Install-Package EventStore.Tools.Infrastructure  
```

In the config file of your Host program, add the following EventStore settings to connect to a single node  
```xml 
<appSettings>  
    <add key="EventStoreUserName" value="youruser" />  
    <add key="EventStorePassword" value="yourpassword" />  
    <add key="EventStoreNode1HostName" value="127.0.0.1" />  
    <add key="EventStoreNode1TcpPort" value="1113" />  
    <add key="EventStoreNode1HttpPort" value="2113" />  
</appSettings>  
```
For multinode configuration add any other node element following the naming convention (EventStoreNode2..., EventStoreNode3...)  

This library is a toolset. The following features can be combined or you can just pick the one that you need.

#Create an EventStore Connection

```c#
var connection = EventStore.Tools.Infrastructure.Configuration.CreateConnection("MyAdapterConnection");
```

#Use the DomainRepository

The IDomainRepository interface expose two methods: 'Save' and GetById. The Save method take an IAggregate as parameter. This interface is also exposed by the library and can be used combined with the AggregateBase class.  
  
There are two available repositories:  
1) EventStoreDomainRepository: this is used to interact with an EventStore service  
2) InMemoryDomainRepository: this is used for testing  
  
example creating an EventStoreDomainRepository
```c#
var repository = new EventStoreDomainRepository("MyApp", Configuration.CreateConnection("MyAdapterConnection"));
```

#Create message handlers with the IHandle interface  
  
  The IHandle interface offer the possibility to compose in a class all the handling functions for events and messages. You can create more than one class using this interface depending on the business features and boundaries.  
  
example implementing this interface in a service class  
```c#
internal class AssociateAccountService : 
        IHandle<CreateAssociateAccount>, 
        IHandle<RegisterIncome>,
        IHandle<RegisterExpense>
    { 
    //...
    
```

example implementing a IBus interface to dispatch messages (events and commands) to the service class  
```c# 
public class DomainEntry
    {
        private readonly Bus _bus;

        public DomainEntry(IDomainRepository domainRepository)
        {
            _bus = CreateBus(domainRepository);
        }
        private Bus CreateBus(IDomainRepository domainRepository)
        {
            var bus = new Bus(domainRepository, null, null);

            var associateAccountCommandHandler = new AssociateAccountService(domainRepository);
            bus.RegisterCommandHandler<CreateAssociateAccount>(associateAccountCommandHandler);
            bus.RegisterCommandHandler<RegisterIncome>(associateAccountCommandHandler);
            bus.RegisterCommandHandler<RegisterExpense>(associateAccountCommandHandler);

            return bus;
        }
        public void Send<TCommand>(TCommand command) where TCommand : ICommand
        {
            _bus.Send(command);
        }
    }
```
