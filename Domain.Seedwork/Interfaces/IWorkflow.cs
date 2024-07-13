namespace Domain.Seedwork.Interfaces;

public interface IWorkflow<out TEntity, TKey>
    where TEntity : Entity<TKey>
{
    TEntity Entity { get; }
}