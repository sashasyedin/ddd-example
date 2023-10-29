using System.Linq.Expressions;

namespace Ddd.Core.SeedWork;

public abstract class Specification<T>
{
    private Action _proceedWithAction = () => { };

    protected internal abstract string[] ErrorMessages { get; }

    public bool IsSatisfiedBy(T entity, out string message)
    {
        var predicate = ToExpression().Compile();

        var result = predicate(entity);
        if (result)
            _proceedWithAction();

        message = string.Join(OuterStringSeparator, ErrorMessages);
        return result;
    }

    public Specification<T> ProceedWith(Action action)
    {
        _proceedWithAction = action;
        return this;
    }

    public Specification<T> And(Specification<T> specification)
        => new AndSpecification<T>(this, specification);

    protected internal abstract Expression<Func<T, bool>> ToExpression();
}