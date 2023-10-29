using System.Linq.Expressions;

namespace Ddd.Core.SeedWork;

public class AndSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;

    public AndSpecification(Specification<T> left, Specification<T> right)
    {
        _right = right;
        _left = left;
    }

    protected internal override string[] ErrorMessages
        => _left.ErrorMessages.Concat(_right.ErrorMessages).ToArray();

    protected internal override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpression = _left.ToExpression();
        var rightExpression = _right.ToExpression();

        var paramExpr = Expression.Parameter(typeof(T));
        var andExpression = Expression.AndAlso(leftExpression.Body, rightExpression.Body);

        andExpression = (BinaryExpression)new ParameterReplacer(paramExpr).Visit(andExpression);

        return Expression.Lambda<Func<T, bool>>(andExpression, paramExpr);
    }

    private class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _parameter;

        public ParameterReplacer(ParameterExpression parameter)
            => _parameter = parameter;

        protected override Expression VisitParameter(ParameterExpression node)
            => base.VisitParameter(_parameter);
    }
}