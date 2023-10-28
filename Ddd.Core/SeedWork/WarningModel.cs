namespace Ddd.Core.SeedWork;

public record WarningModel
{
    public long AggregateVersion { get; init; }
    public string Message { get; init; } = Unknown;
}