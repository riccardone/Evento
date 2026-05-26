# Evento

[![CI](https://github.com/riccardone/Evento/actions/workflows/ci.yml/badge.svg)](https://github.com/riccardone/Evento/actions/workflows/ci.yml)

This C# .Net Standard library can be used to build components based on Event Sourcing patterns. It can be considered a small toolbox as it provides few types and a Repository. The types are Command and Event and can help to better structure the flow of your data. It's lightweight and you can just copy and paste the code in your component codebase to avoid adding a dependency. In that way you can easily tailor made the features on your needs.   
  
It is not related to a particular storage. There is a Repository implementation using KurrentDb formerly called EventStore https://github.com/kurrent-io/KurrentDB 
  
You can find more info in my blog http://www.dinuzzo.co.uk/2017/04/28/domain-driven-design-event-sourcing-and-micro-services-explained-for-developers/  
  
The 'Evento' library has no-dependencies therefore it can be referenced by Domain projects and Application Service projects.
You can see a Sample project showing how to use this library https://github.com/riccardone/Evento.Samples  

## Packages

| Package | Target | Description |
|---|---|---|
| `Evento` | netstandard2.0 | Core types: Command, Event, AggregateBase, IDomainRepository |
| `Evento.Repository` | netstandard2.0 | Legacy. EventStoreDomainRepository using the TCP `EventStore.Client` |
| `Evento.Repository.Grpc` | net8.0 | Recommended. EventStoreDomainRepository using `KurrentDB.Client` (gRPC) |

## Installation

### Core library
```
PM> Install-Package Evento
```
```
dotnet add package Evento
```

### gRPC repository (recommended)
```
PM> Install-Package Evento.Repository.Grpc
```
```
dotnet add package Evento.Repository.Grpc
```

### Legacy TCP repository
```
PM> Install-Package Evento.Repository
```
```
dotnet add package Evento.Repository
```

For old EventStore 5.x please install the legacy package pinned to v5:
```
PM> Install-Package Evento.Repository -Version 5.1.0
```

## Cutting a Release

Releases are published to NuGet via GitHub Actions and triggered manually.

### Prerequisites
- A `NUGET_API_KEY` secret must be set in the repository under *Settings → Secrets and variables → Actions*

### Publish Evento
1. Go to **Actions → Publish Evento → Run workflow**
2. Leave the version field **empty** to auto-bump the patch (e.g. `5.2.0` → `5.2.1`), or enter a specific version (e.g. `5.3.0`) for a minor/major bump
3. Click **Run workflow**

The workflow will:
- Update the version in the `.csproj`
- Build and pack the NuGet package
- Push it to [NuGet.org](https://www.nuget.org)
- Commit the version bump and create an `evento-<version>` tag

### Publish Evento.Repository.Grpc (recommended)
Same steps as above but use **Actions → Publish Evento.Repository.Grpc**.  
A `repository-grpc-<version>` tag is created on completion.

### Publish Evento.Repository (legacy)
Same steps as above but use **Actions → Publish Evento.Repository**.  
A `repository-<version>` tag is created on completion.

---

## Use the EventStore DomainRepository

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
