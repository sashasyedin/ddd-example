namespace Domain.Seedwork.Interfaces;

public interface IDomainEvent<out TKey> : IDomainObject
{
    TKey EntityId { get; }
    TKey AggregateId { get; }
    long AggregateVersion { get; }
    long Timestamp { get; }
}