namespace Ddd.Core.SeedWork;

public interface IAggregateRoot<out TKey>
{
    TKey Id { get; }
    long Version { get; }
    long CreatedAtTimestamp { get; }
    long UpdatedAtTimestamp { get; }
}