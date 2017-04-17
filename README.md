# EventStore.Tools.Infrastructure
This .Net 4.0 C# library can be used to build event sourced components interacting with GetEventStore https://github.com/EventStore/EventStore 
There are not external dependencies therefore it can be referenced by Domain projects and Application Service projects.
  
You can reference this project using Nuget  
```
PM> Install-Package EventStore.Tools.Infrastructure  
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

# EventStore.Tools.Infrastructure.Repository
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

# Thank You
A big thank you to Greg Young and the EventStore team for giving us such a good database
