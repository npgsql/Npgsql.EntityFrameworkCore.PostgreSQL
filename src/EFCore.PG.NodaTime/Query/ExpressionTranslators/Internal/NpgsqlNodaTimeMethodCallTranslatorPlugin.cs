using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Expressions.Internal;

// ReSharper disable once CheckNamespace
namespace Npgsql.EntityFrameworkCore.PostgreSQL.NodaTime
{
    /// <summary>
    /// Provides translation services for <see cref="NodaTime"/> members.
    /// </summary>
    /// <remarks>
    /// See: https://www.postgresql.org/docs/current/static/functions-datetime.html
    /// </remarks>
    public class NpgsqlNodaTimeMethodCallTranslatorPlugin : IMethodCallTranslatorPlugin
    {
        public NpgsqlNodaTimeMethodCallTranslatorPlugin([NotNull] ISqlExpressionFactory sqlExpressionFactory)
        {
            Translators = new IMethodCallTranslator[]
            {
                new NpgsqlNodaTimeMethodCallTranslator((NpgsqlSqlExpressionFactory)sqlExpressionFactory),
            };
        }

        public virtual IEnumerable<IMethodCallTranslator> Translators { get; }
    }

    /// <summary>
    /// Provides translation services for NodaTime method calls.
    /// </summary>
    public class NpgsqlNodaTimeMethodCallTranslator : IMethodCallTranslator
    {
        readonly NpgsqlSqlExpressionFactory _sqlExpressionFactory;

        /// <summary>
        /// The static method info for <see cref="T:SystemClock.GetCurrentInstant()"/>.
        /// </summary>
        [NotNull] static readonly MethodInfo GetCurrentInstant =
            typeof(SystemClock).GetRuntimeMethod(nameof(SystemClock.GetCurrentInstant), Type.EmptyTypes);

        /// <summary>
        /// The mapping of supported method translations.
        /// </summary>
        [NotNull] static readonly Dictionary<MethodInfo, string> PeriodMethodMap = new()
        {
            { typeof(Period).GetRuntimeMethod(nameof(Period.FromYears),        new[] { typeof(int) }),  "years" },
            { typeof(Period).GetRuntimeMethod(nameof(Period.FromMonths),       new[] { typeof(int) }),  "months" },
            { typeof(Period).GetRuntimeMethod(nameof(Period.FromWeeks),        new[] { typeof(int) }),  "weeks" },
            { typeof(Period).GetRuntimeMethod(nameof(Period.FromDays),         new[] { typeof(int) }),  "days" },
            { typeof(Period).GetRuntimeMethod(nameof(Period.FromHours),        new[] { typeof(long) }), "hours" },
            { typeof(Period).GetRuntimeMethod(nameof(Period.FromMinutes),      new[] { typeof(long) }), "mins" },
            { typeof(Period).GetRuntimeMethod(nameof(Period.FromSeconds),      new[] { typeof(long) }), "secs" },
            //{ typeof(Period).GetRuntimeMethod(nameof(Period.FromMilliseconds), new[] { typeof(long) }), "" },
            //{ typeof(Period).GetRuntimeMethod(nameof(Period.FromNanoseconds),  new[] { typeof(long) }), "" },
        };

        static readonly bool[][] TrueArrays =
        {
            Array.Empty<bool>(),
            new[] { true },
            new[] { true, true },
        };

        public NpgsqlNodaTimeMethodCallTranslator([NotNull] NpgsqlSqlExpressionFactory sqlExpressionFactory)
            => _sqlExpressionFactory = sqlExpressionFactory;

#pragma warning disable EF1001
        /// <inheritdoc />
        public virtual SqlExpression Translate(
            SqlExpression instance,
            MethodInfo method,
            IReadOnlyList<SqlExpression> arguments,
            IDiagnosticsLogger<DbLoggerCategory.Query> logger)
        {
            if (method == GetCurrentInstant)
            {
                return _sqlExpressionFactory.AtTimeZone(
                    _sqlExpressionFactory.Function(
                        "NOW",
                        Array.Empty<SqlExpression>(),
                        nullable: false,
                        argumentsPropagateNullability: Array.Empty<bool>(),
                        method.ReturnType),
                    _sqlExpressionFactory.Constant("UTC"),
                    method.ReturnType);
            }

            var declaringType = method.DeclaringType;
            if (declaringType == typeof(Period))
            {
                return PeriodMethodMap.TryGetValue(method, out var datePart)
                    ? PostgresFunctionExpression.CreateWithNamedArguments(
                        "MAKE_INTERVAL",
                        new[] { _sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[0]) },
                        new[] { datePart },
                        nullable: true,
                        argumentsPropagateNullability: TrueArrays[1],
                        builtIn: true,
                        typeof(Period),
                        typeMapping: null)
                    : null;
            }

            if (declaringType == typeof(DateInterval))
            {
                switch (method.Name)
                {
                    case nameof(DateInterval.Contains):
                        return _sqlExpressionFactory.Contains(instance, arguments[0]);
                    case nameof(DateInterval.Intersection):
                        return _sqlExpressionFactory.MakePostgresBinary(PostgresExpressionType.RangeIntersect, instance, arguments[0]);
                    case nameof(DateInterval.Union):
                        return _sqlExpressionFactory.MakePostgresBinary(PostgresExpressionType.RangeUnion, instance, arguments[0]);
                }
            }

            return null;
        }
#pragma warning restore EF1001
    }
}
