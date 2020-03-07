[![Build Status](https://riccardone.visualstudio.com/Evento/_apis/build/status/riccardone.Evento?branchName=master)](https://riccardone.visualstudio.com/Evento/_build/latest?definitionId=9&branchName=master)

# Evento
This C# .Net Standard library can be used to build components based on Event Sourcing patterns. It can be considered a small toolbox as it provides few types and a Repository. The types are Command and Event and can help to better structure the flow of your data. It's lightweight and you can just copy and paste the code in your component codebase to avoid adding a dependency. In that way you can easily tailor made the features on your needs.   
  
It is not related to a particular storage. There is a Repository implementation using EventStore https://github.com/EventStore/EventStore 
  
You can find more info in my blog http://www.dinuzzo.co.uk/2017/04/28/domain-driven-design-event-sourcing-and-micro-services-explained-for-developers/  
  
The 'Evento' library has no-dependencies therefore it can be referenced by Domain projects and Application Service projects.
You can see a Sample project showing how to use this library https://github.com/riccardone/Evento.Samples  
  
Current EventStore.Client: v5.0.6  
  
You can reference this project using Nuget  
```
PM> Install-Package Evento  
```

# Evento.Repository
There is a working Repository EventStoreDomainRepository with external dependencies to EventStore.Client v5.X and Newton.Json. This library can be referenced and used in the top level host process application and injected into any Application Service that requires an IDomainRepository.  
  
You can reference this project using Nuget  
```
PM> Install-Package Evento.Repository
```  

# Use the EventStore DomainRepository

The IDomainRepository interface expose two methods: 'Save' and GetById. The Save method take an IAggregate as parameter and a correlationId. 
The correlationId is used to link toghether the events that are part of the same conversation. It defines the AggregateId and when the events are stored in EventStore it is used to define the StreamId.
  
example creating an EventStoreDomainRepository with the word 'domain' as category
```c#
var repository = new EventStoreDomainRepository("domain", Configuration.CreateConnection("MyAdapterConnection"));
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
