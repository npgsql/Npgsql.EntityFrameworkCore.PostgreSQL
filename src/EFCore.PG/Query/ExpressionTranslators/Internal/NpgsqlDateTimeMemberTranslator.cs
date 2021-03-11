using System;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using NpgsqlTypes;
using static Npgsql.EntityFrameworkCore.PostgreSQL.Utilities.Statics;

namespace Npgsql.EntityFrameworkCore.PostgreSQL.Query.ExpressionTranslators.Internal
{
    /// <summary>
    /// Provides translation services for <see cref="DateTime"/> members.
    /// </summary>
    /// <remarks>
    /// See: https://www.postgresql.org/docs/current/static/functions-datetime.html
    /// </remarks>
    public class NpgsqlDateTimeMemberTranslator : IMemberTranslator
    {
        readonly NpgsqlSqlExpressionFactory _sqlExpressionFactory;

        public NpgsqlDateTimeMemberTranslator([NotNull] NpgsqlSqlExpressionFactory sqlExpressionFactory)
            => _sqlExpressionFactory = sqlExpressionFactory;

        /// <inheritdoc />
        public virtual SqlExpression Translate(SqlExpression instance,
            MemberInfo member,
            Type returnType,
            IDiagnosticsLogger<DbLoggerCategory.Query> logger)
        {
            var type = member.DeclaringType;
            if (type != typeof(DateTime) && type != typeof(DateTimeOffset) && type != typeof(NpgsqlDateTime) && type != typeof(NpgsqlDate))
                return null;

            return member.Name switch
            {
                nameof(DateTime.Now)       => Now(),
                // Not supporting DateTimeOffset.UtcNow, as there is no valid use-case for it.
                // SELECTing it into .NET via a query can only result in an incorrect value, as Npgsql translates "TIMESTAMP WITH TIME ZONE" to NpgsqlDateTime,
                //   ignoring the offset, at which point it becomes impossible for EF to know what the offset is supposed to be, which is why we just always assume it's local,
                //   which in this case will always be incorrect.
                // When INSERTing the value into a table, PostgreSQL converts the value to UTC and drops the offset info, so there's no point in using UtcNow over Now.
                // When using the value in a WHERE clause, again, there's no point in using UtcNow over Now, because PostgreSQL will properly adjust for offsets
                //   in comparison and arithmetic operations.
                nameof(DateTime.UtcNow) => (member.DeclaringType != typeof(DateTimeOffset))
                    ? _sqlExpressionFactory.AtTimeZone(
                        Now(),
                        _sqlExpressionFactory.Constant("UTC"),
                        returnType)
                    : null,

                nameof(DateTime.Today)     => _sqlExpressionFactory.Function(
                    "date_trunc",
                    new SqlExpression[] { _sqlExpressionFactory.Constant("day"), Now() },
                    nullable: true,
                    argumentsPropagateNullability: TrueArrays[2],
                    returnType),

                nameof(DateTime.Year)      => GetDatePartExpression(instance, "year"),
                nameof(DateTime.Month)     => GetDatePartExpression(instance, "month"),
                nameof(DateTime.DayOfYear) => GetDatePartExpression(instance, "doy"),
                nameof(DateTime.Day)       => GetDatePartExpression(instance, "day"),
                nameof(DateTime.Hour)      => GetDatePartExpression(instance, "hour"),
                nameof(DateTime.Minute)    => GetDatePartExpression(instance, "minute"),
                nameof(DateTime.Second)    => GetDatePartExpression(instance, "second"),

                nameof(DateTime.Millisecond) => null, // Too annoying

                // .NET's DayOfWeek is an enum, but its int values happen to correspond to PostgreSQL
                nameof(DateTime.DayOfWeek) => GetDatePartExpression(instance, "dow", floor: true),

                nameof(DateTime.Date) => _sqlExpressionFactory.Function(
                    "date_trunc",
                    new[] { _sqlExpressionFactory.Constant("day"), instance },
                    nullable: true,
                    argumentsPropagateNullability: TrueArrays[2],
                    returnType),

                // TODO: Technically possible simply via casting to PG time, should be better in EF Core 3.0
                // but ExplicitCastExpression only allows casting to PG types that
                // are default-mapped from CLR types (timespan maps to interval,
                // which timestamp cannot be cast into)
                nameof(DateTime.TimeOfDay) => null,

                // TODO: Should be possible
                nameof(DateTime.Ticks) => null,

                nameof(DateTimeOffset.DateTime) => _sqlExpressionFactory.Convert(instance, typeof(DateTime)),

                _ => null
            };

            SqlFunctionExpression Now()
                => _sqlExpressionFactory.Function(
                    "now",
                    Array.Empty<SqlExpression>(),
                    nullable: false,
                    argumentsPropagateNullability: TrueArrays[0],
                    returnType);
        }

        /// <summary>
        /// Constructs the DATE_PART expression.
        /// </summary>
        /// <param name="e">The member expression.</param>
        /// <param name="partName">The name of the DATE_PART to construct.</param>
        /// <param name="floor">True if the result should be wrapped with FLOOR(...); otherwise, false.</param>
        /// <returns>
        /// The DATE_PART expression.
        /// </returns>
        /// <remarks>
        /// DATE_PART returns doubles, which we floor and cast into ints
        /// This also gets rid of sub-second components when retrieving seconds.
        /// </remarks>
        [NotNull]
        SqlExpression GetDatePartExpression(
            [NotNull] SqlExpression instance,
            [NotNull] string partName,
            bool floor = false)
        {
            var result = _sqlExpressionFactory.Function(
                "date_part",
                new[]
                {
                    _sqlExpressionFactory.Constant(partName),
                    instance
                },
                nullable: true,
                argumentsPropagateNullability: TrueArrays[2],
                typeof(double));

            if (floor)
                result = _sqlExpressionFactory.Function(
                    "floor",
                    new[] { result },
                    nullable: true,
                    argumentsPropagateNullability: TrueArrays[1],
                    typeof(double));

            return _sqlExpressionFactory.Convert(result, typeof(int));
        }
    }
}
