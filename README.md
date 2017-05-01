# Evento
This C# library can be used to build event sourced components interacting with GetEventStore https://github.com/EventStore/EventStore 
  
I wrote a post on my blog for an explanation of the code http://www.dinuzzo.co.uk/2017/04/28/domain-driven-design-event-sourcing-and-micro-services-explained-for-developers/  
  
There are not external dependencies therefore it can be referenced by Domain projects and Application Service projects.
You can see a Sample project showing how to use this library https://github.com/riccardone/EventStore.Tools.Infrastructure.Samples
  
You can reference this project using Nuget  
```
PM> Install-Package Evento  
```

# Create message handlers with the IHandle interface  
  
  The IHandle interface offer the possibility to compose in a class all the command handling functions. You can create more than one class using this interface depending on the business features and boundaries.  
  
example implementing this interface in a service class  
```c#
internal class AssociateAccountService : 
        IHandle<CreateAssociateAccount>, 
        IHandle<RegisterIncome>,
        IHandle<RegisterExpense>
    { 
    //...
    
```

# Evento.Repository
This .Net 4.6.1 C# library contains a concrete EventStoreDomainRepository with external dependencies to EventStore.Client v4.X and Newton.Json v10.X
This library can be referenced and used in the top level host process application and injected into any Application Service that requires an IDomainRepository.  

You can reference this project using Nuget  
```
PM> Install-Package EventStore.Tools.Infrastructure.Repository
```  

# Use the EventStore DomainRepository

The IDomainRepository interface expose two methods: 'Save' and GetById. The Save method take an IAggregate as parameter and a correlationId. 
The correlationId is used to link toghether the events that are part of the same conversation. It defines the AggregateId and when the events are stored in EventStore it is used to define the StreamId.
  
example creating an EventStoreDomainRepository
```c#
var repository = new EventStoreDomainRepository("MyApp", Configuration.CreateConnection("MyAdapterConnection"));
```

# Use AggregateBase and IAggregate to build your Aggregates  

```c#
public class AssociateAccount : AggregateBase
    {
        public override string AggregateId => _correlationId;
        private string _correlationId;
        private Guid _associateId;
        private readonly List<Income> _incomes = new List<Income>();
        private readonly List<Expense> _expenses = new List<Expense>();
        public decimal Balance { get; private set; }

        public AssociateAccount(string correlationId, Guid associateId) : this()
        {
            RaiseEvent(new AssociateAccountCreated(correlationId, associateId));
        }

        public AssociateAccount()
        {
            RegisterTransition<AssociateAccountCreated>(Apply);
            RegisterTransition<IncomeRegistered>(Apply);
            RegisterTransition<ExpenseRegistered>(Apply);
        }
        
        private void Apply(AssociateAccountCreated evt)
        {
            _correlationId = evt.CorrelationId;
            _associateId = evt.AssociateId;
        }

        public static IAggregate Create(CreateAssociateAccount cmd)
        {
            return new AssociateAccount(cmd.CorrelationId, cmd.AssociateId);
        }
        
        // ....
    }
```
# Previous releases
Version 2.xx contains some breaking changes. If you are using the previous version 1.3.xx you can keep using it. There is a branch release-v1.3.8 that contains the latest changes. Migrating to the new 2.xx is not very difficult. I've just removed not relevant part of the library like the DomainEntry, CommandDispatcher, IBus. I also renamed IEvent and ICommand to Event and Command. I removed the asynch save method from the DomainRepository.   
I separated the base Infrastructure code from the concrete EventStoreDomainRepository in order to reference it from the projects without need to attach external dependencies. 
The only project that need to reference the Repository is the top level host process from where you inject the EventStoreDomainRepository in the app service. When I refactored some of my existing component with the new library it took one hour.

# Thank You
A big thank you to Greg Young and the EventStore team for giving us such a good database
