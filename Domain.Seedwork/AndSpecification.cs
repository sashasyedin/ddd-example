using System.Linq.Expressions;

namespace Domain.Seedwork;

public class AndSpecification<T>(Specification<T> left, Specification<T> right) : Specification<T>
{
    public override string[] ErrorMessages => left.ErrorMessages
        .Concat(right.ErrorMessages)
        .ToArray();

    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpression = left.ToExpression();
        var rightExpression = right.ToExpression();

        var paramExpr = Expression.Parameter(typeof(T));
        var andExpression = Expression.AndAlso(leftExpression.Body, rightExpression.Body);

        andExpression = (BinaryExpression)new ParameterReplacer(paramExpr).Visit(andExpression);

        return Expression.Lambda<Func<T, bool>>(andExpression, paramExpr);
    }

    private class ParameterReplacer(ParameterExpression parameter) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => base.VisitParameter(parameter);
    }
}