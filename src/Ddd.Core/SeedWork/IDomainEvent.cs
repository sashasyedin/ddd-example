namespace Ddd.Core.SeedWork;

public interface IDomainEvent<out TKey>
{
    TKey AggregateId { get; }
    long AggregateVersion { get; }
    long Timestamp { get; }
}