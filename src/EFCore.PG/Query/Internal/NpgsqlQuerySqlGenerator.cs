using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Expressions.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Utilities;
using NpgsqlTypes;

namespace Npgsql.EntityFrameworkCore.PostgreSQL.Query.Internal
{
    /// <summary>
    /// The default query SQL generator for Npgsql.
    /// </summary>
    public class NpgsqlQuerySqlGenerator : QuerySqlGenerator
    {
        readonly ISqlGenerationHelper _sqlGenerationHelper;

        /// <summary>
        /// True if null ordering is reversed; otherwise false.
        /// </summary>
        readonly bool _reverseNullOrderingEnabled;

        /// <summary>
        /// The backend version to target. If null, it means the user hasn't set a compatibility version, and the
        /// latest should be targeted.
        /// </summary>
        readonly Version _postgresVersion;

        /// <inheritdoc />
        public NpgsqlQuerySqlGenerator(
            QuerySqlGeneratorDependencies dependencies,
            bool reverseNullOrderingEnabled,
            Version postgresVersion)
            : base(dependencies)
        {
            _sqlGenerationHelper = dependencies.SqlGenerationHelper;
            _reverseNullOrderingEnabled = reverseNullOrderingEnabled;
            _postgresVersion = postgresVersion;
        }

        #region Generators

        /// <inheritdoc />
        protected override void GenerateLimitOffset(SelectExpression selectExpression)
        {
            Check.NotNull(selectExpression, nameof(selectExpression));

            if (selectExpression.Limit != null)
            {
                Sql.AppendLine().Append("LIMIT ");
                Visit(selectExpression.Limit);
            }

            if (selectExpression.Offset != null)
            {
                if (selectExpression.Limit == null)
                    Sql.AppendLine();
                else
                    Sql.Append(' ');

                Sql.Append("OFFSET ");
                Visit(selectExpression.Offset);
            }
        }

        /// <inheritdoc />
        protected override string GenerateOperator(SqlBinaryExpression e)
            => e.OperatorType switch
            {
                // PostgreSQL has a special string concatenation operator: ||
                // We switch to it if the expression itself has type string, or if one of the sides has a
                // string type mapping. Same for full-text search's TsVector.
                ExpressionType.Add when
                    e.Type == typeof(string) || e.Left.TypeMapping?.ClrType == typeof(string) || e.Right.TypeMapping?.ClrType == typeof(string) ||
                    e.Type == typeof(NpgsqlTsVector) || e.Left.TypeMapping?.ClrType == typeof(NpgsqlTsVector) || e.Right.TypeMapping?.ClrType == typeof(NpgsqlTsVector)
                    => " || ",
                ExpressionType.And when e.Type == typeof(bool)   => " AND ",
                ExpressionType.Or  when e.Type == typeof(bool)   => " OR ",
                _ => base.GenerateOperator(e)
            };

        /// <inheritdoc />
        protected override Expression VisitOrdering(OrderingExpression ordering)
        {
            var result = base.VisitOrdering(ordering);

            if (_reverseNullOrderingEnabled)
                Sql.Append(ordering.IsAscending ? " NULLS FIRST" : " NULLS LAST");

            return result;
        }

        /// <inheritdoc />
        protected override void GenerateTop(SelectExpression selectExpression)
        {
            // No TOP() in PostgreSQL, see GenerateLimitOffset
        }

        #endregion

        #region Visitors

        protected override Expression VisitCrossApply(CrossApplyExpression crossApplyExpression)
        {
            Sql.Append("JOIN LATERAL ");

            if (crossApplyExpression.Table is TableExpression table)
            {
                // PostgreSQL doesn't support LATERAL JOIN over table, and it doesn't really make sense to do it - but EF Core
                // will sometimes generate that. #1560
                Sql
                    .Append("(SELECT * FROM ")
                    .Append(_sqlGenerationHelper.DelimitIdentifier(table.Name, table.Schema))
                    .Append(")")
                    .Append(AliasSeparator)
                    .Append(_sqlGenerationHelper.DelimitIdentifier(table.Alias));
            }
            else
                Visit(crossApplyExpression.Table);

            Sql.Append(" ON TRUE");
            return crossApplyExpression;
        }

        protected override Expression VisitOuterApply(OuterApplyExpression outerApplyExpression)
        {
            Sql.Append("LEFT JOIN LATERAL ");

            if (outerApplyExpression.Table is TableExpression table)
            {
                // PostgreSQL doesn't support LATERAL JOIN over table, and it doesn't really make sense to do it - but EF Core
                // will sometimes generate that. #1560
                Sql
                    .Append("(SELECT * FROM ")
                    .Append(_sqlGenerationHelper.DelimitIdentifier(table.Name, table.Schema))
                    .Append(")")
                    .Append(AliasSeparator)
                    .Append(_sqlGenerationHelper.DelimitIdentifier(table.Alias));
            }
            else
                Visit(outerApplyExpression.Table);

            Sql.Append(" ON TRUE");
            return outerApplyExpression;
        }

        /// <inheritdoc />
        protected override Expression VisitSqlBinary(SqlBinaryExpression binary)
        {
            switch (binary.OperatorType)
            {
            case ExpressionType.Add:
            {
                if (_postgresVersion == null || _postgresVersion >= new Version(9, 5))
                    return base.VisitSqlBinary(binary);

                // PostgreSQL 9.4 and below has some weird operator precedence fixed in 9.5 and described here:
                // http://git.postgresql.org/gitweb/?p=postgresql.git&a=commitdiff&h=c6b3c939b7e0f1d35f4ed4996e71420a993810d2
                // As a result we must surround string concatenation with parentheses
                if (binary.Left.Type == typeof(string) &&
                    binary.Right.Type == typeof(string))
                {
                    Sql.Append("(");
                    var exp = base.VisitSqlBinary(binary);
                    Sql.Append(")");
                    return exp;
                }

                return base.VisitSqlBinary(binary);
            }

            case ExpressionType.ArrayIndex:
                return VisitArrayIndex(binary);

            default:
                return base.VisitSqlBinary(binary);
            }
        }

        [NotNull]
        protected virtual Expression VisitArrayIndex([NotNull] SqlBinaryExpression expression)
        {
            Visit(expression.Left);
            Sql.Append('[');
            Visit(expression.Right);
            Sql.Append(']');
            return expression;
        }

        protected override Expression VisitSqlUnary(SqlUnaryExpression sqlUnaryExpression)
        {
            switch (sqlUnaryExpression.OperatorType)
            {
            case ExpressionType.Convert:
            {
                // PostgreSQL supports the standard CAST(x AS y), but also a lighter x::y which we use
                // where there's no precedence issues
                switch (sqlUnaryExpression.Operand)
                {
                case SqlConstantExpression _:
                case SqlParameterExpression _:
                case SqlFunctionExpression _:
                    var storeType = sqlUnaryExpression.TypeMapping.StoreType;
                    if (storeType == "integer")
                        storeType = "INT";  // Shorthand that looks better in SQL

                    Visit(sqlUnaryExpression.Operand);
                    Sql.Append("::");
                    Sql.Append(storeType);
                    return sqlUnaryExpression;
                }

                break;
            }

            // Bitwise complement on networking types
            // TODO: Hack, see #1118
            case ExpressionType.Negate when
                sqlUnaryExpression.Operand.TypeMapping.ClrType == typeof(IPAddress) ||
                sqlUnaryExpression.Operand.TypeMapping.ClrType == typeof((IPAddress, int)) ||
                sqlUnaryExpression.Operand.TypeMapping.ClrType == typeof(PhysicalAddress):
                Sql.Append('~');
                Visit(sqlUnaryExpression.Operand);
                return sqlUnaryExpression;

            // Negation on full-text queries
            // TODO: Hack, see #1118
            case ExpressionType.Negate when sqlUnaryExpression.Operand.TypeMapping.ClrType == typeof(NpgsqlTsQuery):
                Sql.Append("!!");
                Visit(sqlUnaryExpression.Operand);
                return sqlUnaryExpression;

            // EF uses unary Equal and NotEqual to represent is-null checking.
            // These need to be surrounded in parentheses in various cases (e.g. where TRUE = x IS NOT NULL
            case ExpressionType.Equal:
                Sql.Append("(");
                Visit(sqlUnaryExpression.Operand);
                Sql.Append(" IS NULL)");
                return sqlUnaryExpression;

            case ExpressionType.NotEqual:
                Sql.Append("(");
                Visit(sqlUnaryExpression.Operand);
                Sql.Append(" IS NOT NULL)");
                return sqlUnaryExpression;
            }

            return base.VisitSqlUnary(sqlUnaryExpression);
        }

        protected override void GenerateSetOperationOperand(SetOperationBase setOperation, SelectExpression operand)
        {
            // PostgreSQL allows ORDER BY and LIMIT in set operation operands, but requires parentheses
            if (operand.Orderings.Count > 0 || operand.Limit != null)
            {
                Sql.AppendLine("(");
                using (Sql.Indent())
                {
                    Visit(operand);
                }
                Sql.AppendLine().Append(")");
                return;
            }

            base.GenerateSetOperationOperand(setOperation, operand);
        }

        /// <summary>
        /// Produces expressions like: 1 = ANY ('{0,1,2}') or 'cat' LIKE ANY ('{a%,b%,c%}').
        /// </summary>
        [NotNull]
        public virtual Expression VisitArrayAnyAll([NotNull] ArrayAnyAllExpression expression)
        {
            Visit(expression.Operand);
            Sql.Append(' ');
            Sql.Append(expression.Operator);
            Sql.Append(' ');
            Sql.Append(expression.ArrayComparisonType == ArrayComparisonType.All ? "ALL" : "ANY");
            Sql.Append(" (");
            Visit(expression.Array);
            Sql.Append(')');
            return expression;
        }

        /// <summary>
        /// Produces SQL array index expression (e.g. arr[1]).
        /// </summary>
        [NotNull]
        public virtual Expression VisitArrayIndex([NotNull] ArrayIndexExpression expression)
        {
            Visit(expression.Array);
            Sql.Append('[');
            Visit(expression.Index);
            Sql.Append(']');
            return expression;
        }

        /// <summary>
        /// Visits the children of a <see cref="RegexMatchExpression"/>.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>
        /// An <see cref="Expression"/>.
        /// </returns>
        /// <remarks>
        /// See: http://www.postgresql.org/docs/current/static/functions-matching.html
        /// </remarks>
        [NotNull]
        public virtual Expression VisitRegexMatch([NotNull] RegexMatchExpression expression)
        {
            var options = expression.Options;

            Visit(expression.Match);
            Sql.Append(" ~ ");

            // PG regexps are single-line by default
            if (options == RegexOptions.Singleline)
            {
                Visit(expression.Pattern);
                return expression;
            }

            Sql.Append("('(?");
            if (options.HasFlag(RegexOptions.IgnoreCase))
                Sql.Append('i');

            if (options.HasFlag(RegexOptions.Multiline))
                Sql.Append('n');
            else if (!options.HasFlag(RegexOptions.Singleline))
                // In .NET's default mode, . doesn't match newlines but PostgreSQL it does.
                Sql.Append('p');

            if (options.HasFlag(RegexOptions.IgnorePatternWhitespace))
                Sql.Append('x');

            Sql.Append(")' || ");
            Visit(expression.Pattern);
            Sql.Append(')');

            return expression;
        }

        /// <summary>
        /// Visits the children of an <see cref="ILikeExpression"/>.
        /// </summary>
        /// <param name="likeExpression">The expression.</param>
        /// <returns>
        /// An <see cref="Expression"/>.
        /// </returns>
        [NotNull]
        public virtual Expression VisitILike([NotNull] ILikeExpression likeExpression)
        {
            Visit(likeExpression.Match);
            Sql.Append(" ILIKE ");
            Visit(likeExpression.Pattern);

            if (likeExpression.EscapeChar != null)
            {
                Sql.Append(" ESCAPE ");
                Visit(likeExpression.EscapeChar);
            }

            return likeExpression;
        }

        /// <summary>
        /// Visits the children of an <see cref="AtTimeZoneExpression"/>.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>
        /// An <see cref="Expression"/>.
        /// </returns>
        [NotNull]
        public virtual Expression VisitAtTimeZone([NotNull] AtTimeZoneExpression expression)
        {
            Visit(expression.Timestamp);
            Sql.Append(" AT TIME ZONE ");
            Visit(expression.TimeZone);
            return expression;
        }

        /// <summary>
        /// Visits the children of an <see cref="JsonTraversalExpression"/>.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>
        /// An <see cref="Expression"/>.
        /// </returns>
        [NotNull]
        public virtual Expression VisitJsonPathTraversal([NotNull] JsonTraversalExpression expression)
        {
            Visit(expression.Expression);

            if (expression.Path.Length == 1)
            {
                Sql.Append(expression.ReturnsText ? "->>" : "->");
                Visit(expression.Path[0]);
                return expression;
            }

            // Multiple path components
            Sql.Append(expression.ReturnsText ? "#>>" : "#>");

            // Use simplified array literal syntax if all path components are constants for cleaner SQL
            if (expression.Path.All(p => p is SqlConstantExpression))
            {
                Sql
                    .Append("'{")
                    .Append(string.Join(",", expression.Path.Select(p => ((SqlConstantExpression)p).Value)))
                    .Append("}'");
            }
            else
            {
                Sql.Append("ARRAY[");
                for (var i = 0; i < expression.Path.Length; i++)
                {
                    Visit(expression.Path[i]);
                    if (i < expression.Path.Length - 1)
                        Sql.Append(',');
                }
                Sql.Append("]::TEXT[]");
            }

            return expression;
        }

        /// <summary>
        /// Visits the children of a <see cref="SqlCustomBinaryExpression"/>.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>
        /// An <see cref="Expression"/>.
        /// </returns>
        [NotNull]
        public virtual Expression VisitCustomBinary([NotNull] SqlCustomBinaryExpression expression)
        {
            Sql.Append('(');
            Visit(expression.Left);
            Sql.Append(' ');
            Sql.Append(expression.Operator);
            Sql.Append(' ');
            Visit(expression.Right);
            Sql.Append(')');

            return expression;
        }

        /// <summary>
        /// Visits the children of a <see cref="CustomUnaryExpression"/>.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>
        /// An <see cref="Expression"/>.
        /// </returns>
        [NotNull]
        public virtual Expression VisitCustomUnary([NotNull] CustomUnaryExpression expression)
        {
            if (expression.Postfix)
            {
                Visit(expression.Operand);
                Sql.Append(expression.Operator);
            }
            else
            {
                Sql.Append(expression.Operator);
                Visit(expression.Operand);
            }

            return expression;
        }

        /// <inheritdoc />
//        protected override Expression VisitDefault(DefaultExpression e)
//        {
//            // LOWER(range) and UPPER(range) return null on empty or infinite bounds.
//            // When this happens, we need to ensure the database null is coalesced
//            // back to the CLR default bound value (e.g. NpgsqlRange<int>.LowerBound is `int` not `int?`).
//            var instance = e.Type.IsNullableType() ? null : Activator.CreateInstance(e.Type);
//
//            Sql.Append(TypeMappingSource.FindMapping(e.Type).GenerateSqlLiteral(instance));
//
//            return e;
//        }

        /// <summary>
        /// Visits the children of a <see cref="PgFunctionExpression"/>.
        /// </summary>
        /// <param name="sqlFunctionExpression">The expression.</param>
        /// <returns>
        /// An <see cref="Expression"/>.
        /// </returns>
        [NotNull]
        public virtual Expression VisitPgFunction([NotNull] PgFunctionExpression sqlFunctionExpression)
        {
            if (!string.IsNullOrEmpty(sqlFunctionExpression.Schema))
            {
                Sql
                    .Append(_sqlGenerationHelper.DelimitIdentifier(sqlFunctionExpression.Schema))
                    .Append(".");
            }

            // TODO: Quote user-defined function names with upper-case
            Sql
                .Append(sqlFunctionExpression.FunctionName)
                .Append("(");

            GenerateList(sqlFunctionExpression.PositionalArguments, e => Visit(e));

            var hasArguments = sqlFunctionExpression.PositionalArguments.Count > 0 && sqlFunctionExpression.NamedArguments.Count > 0;

            foreach (var kv in sqlFunctionExpression.NamedArguments)
            {
                if (hasArguments)
                    Sql.Append(", ");
                else
                    hasArguments = true;

                Sql.Append(kv.Key).Append(" => ");

                Visit(kv.Value);
            }

            Sql.Append(")");

            return sqlFunctionExpression;
        }

        void GenerateList<T>(
            IReadOnlyList<T> items,
            Action<T> generationAction,
            Action<IRelationalCommandBuilder> joinAction = null)
        {
            joinAction ??= (isb => isb.Append(", "));

            for (var i = 0; i < items.Count; i++)
            {
                if (i > 0)
                {
                    joinAction(Sql);
                }

                generationAction(items[i]);
            }
        }

        #endregion
    }
}
