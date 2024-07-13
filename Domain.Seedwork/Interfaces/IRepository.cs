namespace Domain.Seedwork.Interfaces;

public interface IRepository<TAggregate, in TKey>
    where TAggregate : IAggregateRoot
{
    Task<TAggregate?> GetAsync(TKey id);
    Task SaveAsync(TAggregate aggregate);
}