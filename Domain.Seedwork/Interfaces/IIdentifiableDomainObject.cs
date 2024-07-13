namespace Domain.Seedwork.Interfaces;

public interface IIdentifiableDomainObject<out TKey> : IDomainObject
{
    TKey Id { get; }
}