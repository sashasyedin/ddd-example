namespace Ddd.Core.SeedWork;

public interface IIntegrationEvent<out TKey>
{
    TKey EntityId { get; }
    Guid EventId { get; }
}