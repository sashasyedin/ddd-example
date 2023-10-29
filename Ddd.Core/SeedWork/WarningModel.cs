namespace Ddd.Core.SeedWork;

public record WarningModel
{
    public required long AggregateVersion { get; init; }
    public required string Message { get; init; }
}