using System.Linq.Expressions;

namespace Domain.Seedwork;

public abstract class Specification<T>
{
    public abstract string[] ErrorMessages { get; }

    public abstract Expression<Func<T, bool>> ToExpression();

    public bool IsSatisfiedBy(T entity, out string message)
    {
        var predicate = ToExpression().Compile();

        message = ErrorMessages.Length switch
        {
            0 => "An error has occurred",
            1 => ErrorMessages[0],
            _ => $"One or more errors occurred: {string.Join(" ", ErrorMessages)}"
        };

        return predicate(entity);
    }

    public Specification<T> And(Specification<T> specification)
        => new AndSpecification<T>(this, specification);
}