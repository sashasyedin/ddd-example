using System.Reflection;

namespace Ddd.Core.SeedWork;

/// <summary>
/// Provides functionality common to all aggregates
/// </summary>
/// <typeparam name="TAggregate">Aggregate type</typeparam>
/// <typeparam name="TEvent">Base event type</typeparam>
/// <typeparam name="TKey">Aggregate root id type</typeparam>
public abstract class BaseAggregate<TAggregate, TEvent, TKey> : IAggregateRoot<TKey>
    where TAggregate : class, IAggregateRoot<TKey>
    where TEvent : class, IDomainEvent<TKey>
{
    private readonly Queue<TEvent> _events = new();
    private readonly List<WarningModel> _warnings = new();

    // ReSharper disable StaticMemberInGenericType
    private static readonly ConstructorInfo Ctor;
    private static readonly Dictionary<Type, MethodInfo> EventHandlers = new();

    static BaseAggregate()
    {
        Ctor = typeof(TAggregate).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, new[] { typeof(TKey) })
               ?? throw new ApplicationException("Unable to find private constructor");

        RegisterEventHandlers();
    }

    protected BaseAggregate(TKey id)
        => Id = id;

    /// <summary>
    /// Collection of uncommitted events
    /// </summary>
    public IReadOnlyCollection<TEvent> Events => _events;

    /// <summary>
    /// Collection of cumulative warnings keyed by aggregate version
    /// </summary>
    public IReadOnlyCollection<WarningModel> Warnings => _warnings;

    public TKey Id { get; }
    public long Version { get; private set; }
    public long CreatedAtTimestamp { get; private set; }
    public long UpdatedAtTimestamp { get; private set; }

    /// <summary>
    /// Invalidates uncommitted events
    /// </summary>
    public void ClearEvents()
        => _events.Clear();

    /// <summary>
    /// Applies event to the aggregate instance
    /// </summary>
    /// <param name="event">Event to be applied to the aggregate</param>
    protected void ApplyEvent(TEvent @event)
    {
        try
        {
            EventHandlers[@event.GetType()].Invoke(this, new object[] { @event });

            if (!_events.Any())
                CreatedAtTimestamp = @event.Timestamp;

            _events.Enqueue(@event);

            UpdatedAtTimestamp = @event.Timestamp;
            Version++;
        }
        catch (TargetInvocationException e)
        {
            if (e.InnerException is null)
                throw;

            e.InnerException.Data.Add("EventType", @event.GetType().Name);
            throw e.InnerException;
        }
    }

    /// <summary>
    /// Logs warnings
    /// </summary>
    /// <param name="event">Reference to event</param>
    /// <param name="message">Message</param>
    protected void LogWarning(TEvent @event, string message)
        => _warnings.Add(new()
        {
            AggregateVersion = @event.AggregateVersion,
            Message = message
        });

    /// <summary>
    /// Rehydrates the aggregate
    /// </summary>
    /// <param name="events">Collection of events to be applied to the aggregate</param>
    /// <returns>Aggregate instance based on the given collection of events</returns>
    /// <exception cref="ArgumentException">Incorrect event state</exception>
    /// <exception cref="ApplicationException">Aggregate type inconsistency</exception>
    public static TAggregate Create(IReadOnlyCollection<TEvent> events)
    {
        if (!events.Any())
            throw new ArgumentException("A list of event cannot be empty");

        var aggregateId = events.First().AggregateId;
        if (aggregateId is null || !events.All(@event => aggregateId.Equals(@event.AggregateId)))
            throw new ArgumentException("The aggregate id cannot be unspecified and must be the same for all events");

        if (Ctor.Invoke(new object[] { aggregateId }) is not TAggregate instance)
            throw new ApplicationException("Wrong object type");

        if (instance is not BaseAggregate<TAggregate, TEvent, TKey> baseAggregate)
            throw new ApplicationException("Wrong object type");

        var orderedByVersion = events
            .OrderBy(@event => @event.AggregateVersion)
            .ToImmutableList();

        orderedByVersion.ForEach(@event => baseAggregate.ApplyEvent(@event));

        baseAggregate.ClearEvents();

        return instance;
    }

    private static void RegisterEventHandlers()
    {
        if (EventHandlers.Any())
            return;

        var methodInfos = typeof(TAggregate).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        foreach (var methodInfo in methodInfos)
        {
            if (!methodInfo.Name.Equals("Apply", StringComparison.OrdinalIgnoreCase))
                continue;

            var parameters = methodInfo.GetParameters();
            if (parameters.Length != 1)
                continue;

            if (!parameters[0].ParameterType.IsAssignableTo(typeof(TEvent)))
                continue;

            EventHandlers[parameters[0].ParameterType] = methodInfo;
        }
    }
}