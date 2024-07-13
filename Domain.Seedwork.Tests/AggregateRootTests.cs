using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AutoFixture;
using Domain.Seedwork.Interfaces;
using FluentAssertions;
using Xunit;

namespace Domain.Seedwork.Tests;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
[SuppressMessage("ReSharper", "UnusedMember.Local")]
[SuppressMessage("ReSharper", "UnusedParameter.Local")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
public class AggregateRootTests
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void Create_ExpectArgumentException_WhenPrivateConstructorIsMissing()
    {
        var exception = Assert.Throws<TypeInitializationException>(() => AggregateInvalid.Create(Array.Empty<AggregateEvent>()));

        exception.InnerException.Should().NotBeNull();
        exception.InnerException.Should().BeOfType<ApplicationException>();
        exception.InnerException!.Message.Should().Be("Unable to find private constructor");
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
        var updatedEvent = new AggregateUpdated("123", 1, 300);

        var actual = AggregateValid.Create(new AggregateEvent[]
        {
            createdEvent,
            updatedEvent
        });

        actual.Should().NotBeNull();
        actual.Id.Should().Be("123");
        actual.Version.Should().Be(2);
        actual.DataMember.Should().Be("Some data");
    }

    [Fact]
    public void ApplyEvent_UnderValidCircumstances()
    {
        var sut = new AggregateValid("123", "Any");
        sut.ApplyEvent(new AggregateUpdated("123", sut.Version, 200));

        sut.Should().NotBeNull();
        sut.Id.Should().Be("123");
        sut.Version.Should().Be(2);
        sut.DataMember.Should().Be("Some data");
    }

    #region Aggregates + Domain Events

    private class AggregateInvalid : AggregateRoot<AggregateInvalid, AggregateEvent, string>
    {
        public AggregateInvalid(string id, string dataMember)
            : base(id)
            => ApplyEvent(new AggregateCreated(Id, Version, 100)
            {
                DataMember = dataMember
            });

        public string? DataMember { get; private set; }

        protected void Apply(AggregateCreated _)
        {
        }

        protected void Apply(AggregateUpdated _)
        {
        }

        protected override IReadOnlyDictionary<Type, Type> EventsMap => new Dictionary<Type, Type>
        {
            { typeof(AggregateCreated), typeof(AggregateInvalid) },
            { typeof(AggregateUpdated), typeof(AggregateInvalid) }
        };
    }

    private class AggregateValid : AggregateRoot<AggregateValid, AggregateEvent, string>
    {
        public AggregateValid(string id, string dataMember)
            : this(id)
            => ApplyEvent(new AggregateCreated(Id, Version, 100)
            {
                DataMember = dataMember
            });

        private AggregateValid(string id)
            : base(id)
        {
        }

        public string DataMember { get; private set; } = "Unknown";

        protected void Apply(AggregateCreated _)
        {
        }

        protected void Apply(AggregateUpdated _)
        {
        }

        protected override void OnEventApplied()
        {
            base.OnEventApplied();

            if (DataMember.Equals("Some data"))
                return;

            DataMember = "Some data";
        }

        protected override IReadOnlyDictionary<Type, Type> EventsMap => new Dictionary<Type, Type>
        {
            { typeof(AggregateCreated), typeof(AggregateValid) },
            { typeof(AggregateUpdated), typeof(AggregateValid) }
        };
    }

    private record AggregateEvent(string EntityId, string AggregateId, long AggregateVersion, long Timestamp)
        : IDomainEvent<string>
    {
        protected AggregateEvent(string aggregateId, long aggregateVersion, long timestamp)
            : this(aggregateId, aggregateId, aggregateVersion, timestamp)
        {
        }
    }

    private record AggregateCreated : AggregateEvent
    {
        public AggregateCreated(string aggregateId, long aggregateVersion, long timestamp)
            : base(aggregateId, aggregateVersion, timestamp)
        {
        }

        public string? DataMember { get; init; }
    }

    private record AggregateUpdated : AggregateEvent
    {
        public AggregateUpdated(string aggregateId, long aggregateVersion, long timestamp)
            : base(aggregateId, aggregateVersion, timestamp)
        {
        }
    }

    #endregion
}