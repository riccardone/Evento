# Evento
This C# library can be used to build event sourced components. 

It is not related to a particular storage. It's available in a separate project a concrete Repository class using GetEventStore https://github.com/EventStore/EventStore 
  
You can find more info in my blog http://www.dinuzzo.co.uk/2017/04/28/domain-driven-design-event-sourcing-and-micro-services-explained-for-developers/  
  
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
PM> Install-Package Evento.Repository
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
namespace Domain.Aggregates
{
    public class AssociateAccount : AggregateBase
    {
        public override string AggregateId => CorrelationId;
        private string CorrelationId { get; set; }
        private Guid AssociateId { get; set; }
        private List<Income> Incomes { get; }
        private List<Expense> Expenses { get; }

        public AssociateAccount(Guid associateId, IDictionary<string, string> metadata) : this()
        {
            RaiseEvent(new AssociateAccountCreated(associateId, metadata));
        }

        public AssociateAccount()
        {
            Incomes = new List<Income>();
            Expenses = new List<Expense>();
            RegisterTransition<AssociateAccountCreated>(Apply);
            RegisterTransition<IncomeRegistered>(Apply);
            RegisterTransition<ExpenseRegistered>(Apply);
        }
        private void Apply(AssociateAccountCreated evt)
        {
            CorrelationId = evt.Metadata["$correlationId"];
            AssociateId = evt.AssociateId;
        }
        public static IAggregate Create(CreateAssociateAccount createAssociateAccount)
        {
            Ensure.NotNull(createAssociateAccount, nameof(createAssociateAccount));
            Ensure.NotEmptyGuid(createAssociateAccount.AssociateId, nameof(createAssociateAccount.AssociateId));

            return new AssociateAccount(createAssociateAccount.AssociateId, createAssociateAccount.Metadata);
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
