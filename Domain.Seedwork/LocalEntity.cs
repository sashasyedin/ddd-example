using Domain.Seedwork.Interfaces;

namespace Domain.Seedwork;

public abstract class LocalEntity<TAggregateRoot, TEvent, TKey>(TKey id, TAggregateRoot aggregateRoot) : Entity<TKey>(id)
    where TAggregateRoot : AggregateRoot<TAggregateRoot, TEvent, TKey>
    where TEvent : class, IDomainEvent<TKey>
{
    protected TAggregateRoot AggregateRoot { get; } = aggregateRoot;
}