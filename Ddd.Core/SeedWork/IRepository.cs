namespace Ddd.Core.SeedWork;

public interface IRepository<TAggregate, in TKey>
    where TAggregate : IAggregateRoot<TKey>
{
    Task<TAggregate?> GetAsync(TKey id);
    Task SaveAsync(TAggregate aggregate);
}