using System.Collections.Generic;
using System.Linq;
using Evento.Repository;
using Evento.Tests.Fakes;
using EventStore.ClientAPI;
using Moq;
using NUnit.Framework;

namespace Evento.Tests;

[TestFixture]
public class DomainRepositoryTests
{
    [Test]
    public void When_I_save_an_aggregate_I_should_receive_the_saved_events()
    {
        // Assign
        const string correlationId = "correlationidexample-123";
        const string testString = "test";
        var cmd = new CreateFakeCommand(testString, new Dictionary<string, string>{{"$correlationId", correlationId}});
        var mockConnection = new Mock<IEventStoreConnection>();
        var repository = new EventStoreDomainRepository("domain", mockConnection.Object);

        // Act
        var results = repository.Save(new FakeHandler().Handle(cmd));

        // Assert
        Assert.IsTrue(((FakeAggregateCreated)results.Single()).Metadata["$correlationId"].Equals(correlationId));
    }
    
    [Test]
    public void CanUseCustomMapping()
    {
        // // Assign
        // const string eventTypeName = "CustomEventType";
        // const string correlationId = "correlationidexample-123";
        // var metadata = new Dictionary<string, string>
        // {
        //     { "$correlationId", correlationId },
        //     { EventStoreDomainRepository.EventClrTypeHeader, typeof(CustomEvent).AssemblyQualifiedName! },
        //     { "type", eventTypeName },
        //     { "created", DateTime.UtcNow.Ticks.ToString() },
        //     { "content-type", "application/json" }
        // };
        // var resolvedEvent = CreateResolvedEvent(eventTypeName, metadata);
        // var mockConnection = new Mock<IEventStoreConnection>();
        // mockConnection
        //     .Setup(
        //         x => x.ReadStreamEventsForwardAsync(
        //             It.IsAny<string>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<UserCredentials?>()))
        //     .ReturnsAsync(
        //         CreateStreamEventsSlice(
        //             SliceReadStatus.Success, "stream", StreamPosition.Start, ReadDirection.Forward, 
        //             new[] { resolvedEvent }, StreamPosition.Start + 1, StreamPosition.Start + 1, false));
        //
        // var customMapping = new Dictionary<string, Type> { { eventTypeName, typeof(CustomEvent) } };
        // var repository = new EventStoreDomainRepository("domain", mockConnection.Object, customMapping);
        //
        // // Act
        // var result = repository.GetById<CustomAggregate>("test-id");
        //
        // // Assert
        // Assert.IsNotNull(result);
        // Assert.IsTrue(result.Events.OfType<CustomEvent>().Any());
    }
}