using System.Reflection;

namespace Domain.Seedwork;

[Serializable]
public abstract class Enumeration(int value, string name) : IComparable
{
    public int Value { get; } = value;
    public string Name { get; } = name;

    public override string ToString()
        => Name;

    public static IEnumerable<T> GetAll<T>()
        where T : Enumeration
        => typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(f => f.GetValue(null))
            .Cast<T>();

    public override bool Equals(object? other)
    {
        if (other is not Enumeration otherValue)
            return false;

        var typeMatches = GetType() == other.GetType();
        var valueMatches = Value.Equals(otherValue.Value);

        return typeMatches && valueMatches;
    }

    public override int GetHashCode()
        => Value.GetHashCode();

    public static int AbsoluteDifference(Enumeration firstValue, Enumeration secondValue)
    {
        var absoluteDifference = Math.Abs(firstValue.Value - secondValue.Value);
        return absoluteDifference;
    }

    public static T FromValue<T>(int value)
        where T : Enumeration
    {
        const string description = "value";
        var matchingItem = Parse<T, int>(value, description, item => item.Value == value);
        return matchingItem;
    }

    public static T FromName<T>(string name)
        where T : Enumeration
    {
        const string description = "name";
        var matchingItem = Parse<T, string>(name, description, item => item.Name == name);
        return matchingItem;
    }

    private static TEnum Parse<TEnum, TEntity>(TEntity entity, string description, Func<TEnum, bool> predicate)
        where TEnum : Enumeration
    {
        var matchingItem = GetAll<TEnum>().FirstOrDefault(predicate);
        if (matchingItem is null)
            throw new InvalidOperationException($"'{entity}' is not a valid {description} in {typeof(TEnum)}");

        return matchingItem;
    }

    public int CompareTo(object? other)
        => other switch
        {
            null => 1,
            Enumeration enumeration => Value.CompareTo(enumeration.Value),
            _ => throw new ArgumentException("An Enumeration object is required for comparison")
        };
}