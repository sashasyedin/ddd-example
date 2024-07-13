using System.Reflection;
using Domain.Seedwork.Interfaces;

namespace Domain.Seedwork;

/// <summary>
/// Provides functionality common to all aggregate roots
/// </summary>
/// <typeparam name="TAggregateRoot">Aggregate root type</typeparam>
/// <typeparam name="TEvent">Base event type</typeparam>
/// <typeparam name="TKey">Id type</typeparam>
[SuppressMessage("ReSharper", "StaticMemberInGenericType")]
public abstract class AggregateRoot<TAggregateRoot, TEvent, TKey>(TKey id) : Entity<TKey>(id), IAggregateRoot
    where TAggregateRoot : AggregateRoot<TAggregateRoot, TEvent, TKey>
    where TEvent : class, IDomainEvent<TKey>
{
    private readonly Queue<TEvent> _events = new();
    private readonly Dictionary<(Type, TKey), Entity<TKey>> _entities = new();
    private readonly List<WarningModel> _warnings = new();

    private static readonly ConstructorInfo Ctor;
    private static readonly Dictionary<Type, MethodInfo> EventHandlers = new();

    static AggregateRoot()
    {
        Ctor = typeof(TAggregateRoot).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, [typeof(TKey)])
               ?? throw new ApplicationException("Unable to find private constructor");

        RegisterEventHandlers();
    }

    /// <summary>
    /// Collection of uncommitted events
    /// </summary>
    public IReadOnlyCollection<TEvent> Events => _events;

    /// <summary>
    /// Collection of cumulative warnings keyed by aggregate version
    /// </summary>
    public IReadOnlyCollection<WarningModel> Warnings => _warnings;

    public long Version { get; private set; }
    public long CreatedAtTimestamp { get; private set; }
    public long UpdatedAtTimestamp { get; private set; }

    [NotNull] public TEvent? LastAppliedEvent { get; private set; }

    /// <summary>
    /// A dictionary that maps event types (keys) to the corresponding entity types (values) in which the events are triggered
    /// </summary>
    protected abstract IReadOnlyDictionary<Type, Type> EventsMap { get; }

    /// <summary>
    /// Invoked after each event in the aggregate is processed
    /// </summary>
    protected virtual void OnEventApplied()
    {
    }

    /// <summary>
    /// Rehydrates the aggregate
    /// </summary>
    /// <param name="events">Collection of events to be applied to the aggregate</param>
    /// <returns>Aggregate instance based on the given collection of events</returns>
    /// <exception cref="ArgumentException">Incorrect event state</exception>
    /// <exception cref="ApplicationException">Aggregate type inconsistency</exception>
    public static TAggregateRoot Create(IReadOnlyCollection<TEvent> events)
    {
        if (events.Count is 0)
            throw new ArgumentException("A list of event cannot be empty");

        var aggregateId = events.First().AggregateId;
        if (aggregateId is null || !events.All(@event => aggregateId.Equals(@event.AggregateId)))
            throw new ArgumentException("The aggregate id cannot be unspecified and must be the same for all events");

        if (Ctor.Invoke([aggregateId]) is not TAggregateRoot instance)
            throw new ApplicationException("Wrong object type");

        if (instance is not AggregateRoot<TAggregateRoot, TEvent, TKey> baseAggregate)
            throw new ApplicationException("Wrong object type");

        var orderedByVersion = events
            .OrderBy(@event => @event.AggregateVersion)
            .ToImmutableList();

        orderedByVersion.ForEach(@event => baseAggregate.ApplyEvent(@event));

        baseAggregate.ClearEvents();

        return instance;
    }

    /// <summary>
    /// Applies an event to the aggregate root
    /// </summary>
    /// <param name="event">Event to be applied to the aggregate root</param>
    public void ApplyEvent(TEvent @event)
    {
        try
        {
            var eventType = @event.GetType();
            if (!EventsMap.ContainsKey(eventType))
                return;

            if (EventHandlers.TryGetValue(eventType, out var methodInfo))
                methodInfo.Invoke(GetEntityByEvent(@event), [@event]);

            AddEvent(@event);
        }
        catch (TargetInvocationException e)
        {
            if (e.InnerException is null)
                throw;

            const string dataKey = "EventType";
            e.InnerException.Data.Add(dataKey, @event.GetType().Name);
            throw e.InnerException;
        }
        finally
        {
            OnEventApplied();
        }
    }

    /// <summary>
    /// Invalidates uncommitted events
    /// </summary>
    public void ClearEvents()
        => _events.Clear();

    /// <summary>
    /// Adds an entity to the aggregate root
    /// </summary>
    /// <param name="entity">The entity to add</param>
    protected void AddEntity(Entity<TKey> entity)
        => _entities[(entity.GetType(), entity.Id)] = entity;

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

    private static void RegisterEventHandlers()
    {
        if (EventHandlers.Count != 0)
            return;

        var methodInfos = GetApplyMethods(typeof(TAggregateRoot));
        foreach (var methodInfo in methodInfos)
        {
            var parameters = methodInfo.GetParameters();
            if (parameters.Length != 1)
                continue;

            if (!parameters[0].ParameterType.IsAssignableTo(typeof(TEvent)))
                continue;

            EventHandlers[parameters[0].ParameterType] = methodInfo;
        }
    }

    private static List<MethodInfo> GetApplyMethods(Type type)
    {
        const string applyMethodName = "Apply";

        var applyMethods = new List<MethodInfo>();

        var entityTypes = type.Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } &&
                        t.Namespace == type.Namespace &&
                        IsDerivedFromEntity(t));

        foreach (var entityType in entityTypes)
        {
            applyMethods.AddRange(entityType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.Name is applyMethodName));
        }

        return applyMethods;
    }

    private static bool IsDerivedFromEntity(Type type)
    {
        while (type is not null && type != typeof(object))
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Entity<>))
                return true;

            if (type.BaseType is null)
                return false;

            type = type.BaseType;
        }

        return false;
    }

    private void AddEvent(TEvent @event)
    {
        if (_events.Count is 0)
            CreatedAtTimestamp = @event.Timestamp;

        _events.Enqueue(@event);

        LastAppliedEvent = @event;
        UpdatedAtTimestamp = @event.Timestamp;
        Version++;
    }

    private Entity<TKey> GetEntityByEvent(TEvent @event)
    {
        var entityType = EventsMap[@event.GetType()];
        return _entities.TryGetValue((entityType, @event.EntityId), out var entity) ? entity : this;
    }
}