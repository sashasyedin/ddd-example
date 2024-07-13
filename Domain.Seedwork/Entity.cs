using Domain.Seedwork.Interfaces;

namespace Domain.Seedwork;

public abstract class Entity<TKey>(TKey id) : IIdentifiableDomainObject<TKey>
{
    public TKey Id { get; } = id;
}