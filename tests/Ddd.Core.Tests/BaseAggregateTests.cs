using Ddd.Core.SeedWork;

namespace Ddd.Core.Tests;

public class BaseAggregateTests
{
    [Fact]
    public void Create_ExpectArgumentException_WhenPrivateConstructorIsMissing()
    {
        var exception = Assert.Throws<TypeInitializationException>(() => AggregateInvalid.Create(Array.Empty<AggregateEvent>()));

        exception.InnerException.Should().NotBeNull();
        exception.InnerException.Should().BeOfType<ApplicationException>();
        exception.InnerException.Message.Should().Be("Unable to find private constructor");
    }

    [Fact]
    public void Create_ExpectArgumentException_WhenEventsSequenceIsEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(() => AggregateValid.Create(Array.Empty<AggregateEvent>()));

        exception.Message.Should().Be("A list of event cannot be empty");
    }

    [Fact]
    public void Create_ExpectArgumentException_WhenInconsistentAggregateIds()
    {
        var createdEvent = new AggregateCreated("123", 0, 100);
        var updatedEvent = new AggregateUpdated("456", 1, 200);

        var exception = Assert.Throws<ArgumentException>(
            () => AggregateValid.Create(new AggregateEvent[]
            {
                createdEvent,
                updatedEvent
            }));

        exception.Message.Should().Be("The aggregate id cannot be unspecified and must be the same for all events");
    }

    [Fact]
    public void Create_UnderValidCircumstances()
    {
        var createdEvent = new AggregateCreated("123", 0, 100);
        var updatedEvent = new AggregateUpdated("123", 1, 200);

        var actual = AggregateValid.Create(new AggregateEvent[]
        {
            createdEvent,
            updatedEvent
        });

        actual.Should().NotBeNull();
        actual.Id.Should().Be("123");
        actual.Version.Should().Be(2);
    }

    private class AggregateInvalid : BaseAggregate<AggregateInvalid, AggregateEvent, string>
    {
        public AggregateInvalid(string id, string dataMember)
            : base(id)
            => ApplyEvent(new AggregateCreated(Id, Version, 100)
            {
                /* Set dataMember */
            });

        public string DataMember { get; private set; }

        protected void Apply(AggregateCreated _)
        {
        }

        protected void Apply(AggregateUpdated _)
        {
        }
    }

    private class AggregateValid : BaseAggregate<AggregateValid, AggregateEvent, string>
    {
        public AggregateValid(string id, string dataMember)
            : base(id)
            => ApplyEvent(new AggregateCreated(Id, Version, 100)
            {
                /* Set dataMember */
            });

        private AggregateValid(string id)
            : base(id)
        {
        }

        public string DataMember { get; private set; }

        protected void Apply(AggregateCreated _)
        {
        }

        protected void Apply(AggregateUpdated _)
        {
        }
    }

    private record AggregateEvent(string AggregateId, long AggregateVersion, long Timestamp)
        : IDomainEvent<string>;

    private record AggregateCreated(string AggregateId, long AggregateVersion, long Timestamp)
        : AggregateEvent(AggregateId, AggregateVersion, Timestamp);

    private record AggregateUpdated(string AggregateId, long AggregateVersion, long Timestamp)
        : AggregateEvent(AggregateId, AggregateVersion, Timestamp);
}