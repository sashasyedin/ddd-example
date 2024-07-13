namespace Domain.Seedwork.Interfaces;

public interface IAggregateRoot
{
    long Version { get; }
    long CreatedAtTimestamp { get; }
    long UpdatedAtTimestamp { get; }
}