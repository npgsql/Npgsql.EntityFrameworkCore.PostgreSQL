using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Internal;

namespace Npgsql.EntityFrameworkCore.PostgreSQL.Query.ExpressionTranslators.Internal
{
    /// <summary>
    /// Provides expression translation for <see cref="DateTime"/> addition methods.
    /// </summary>
    public class NpgsqlDateTimeMethodTranslator : IMethodCallTranslator
    {
        /// <summary>
        /// The mapping of supported AddXXX() method translations.
        /// </summary>
        [NotNull] static readonly Dictionary<MethodInfo, string> MethodInfoDatePartMapping = new()
        {
            { typeof(DateTime).GetRuntimeMethod(nameof(DateTime.AddYears), new[] { typeof(int) }), "years" },
            { typeof(DateTime).GetRuntimeMethod(nameof(DateTime.AddMonths), new[] { typeof(int) }), "months" },
            { typeof(DateTime).GetRuntimeMethod(nameof(DateTime.AddDays), new[] { typeof(double) }), "days" },
            { typeof(DateTime).GetRuntimeMethod(nameof(DateTime.AddHours), new[] { typeof(double) }), "hours" },
            { typeof(DateTime).GetRuntimeMethod(nameof(DateTime.AddMinutes), new[] { typeof(double) }), "mins" },
            { typeof(DateTime).GetRuntimeMethod(nameof(DateTime.AddSeconds), new[] { typeof(double) }), "secs" },
            //{ typeof(DateTime).GetRuntimeMethod(nameof(DateTime.AddMilliseconds), new[] { typeof(double) }), "milliseconds" },
            { typeof(DateTimeOffset).GetRuntimeMethod(nameof(DateTimeOffset.AddYears), new[] { typeof(int) }), "years" },
            { typeof(DateTimeOffset).GetRuntimeMethod(nameof(DateTimeOffset.AddMonths), new[] { typeof(int) }), "months" },
            { typeof(DateTimeOffset).GetRuntimeMethod(nameof(DateTimeOffset.AddDays), new[] { typeof(double) }), "days" },
            { typeof(DateTimeOffset).GetRuntimeMethod(nameof(DateTimeOffset.AddHours), new[] { typeof(double) }), "hours" },
            { typeof(DateTimeOffset).GetRuntimeMethod(nameof(DateTimeOffset.AddMinutes), new[] { typeof(double) }), "mins" },
            { typeof(DateTimeOffset).GetRuntimeMethod(nameof(DateTimeOffset.AddSeconds), new[] { typeof(double) }), "secs" },
            //{ typeof(DateTimeOffset).GetRuntimeMethod(nameof(DateTimeOffset.AddMilliseconds), new[] { typeof(double) }), "milliseconds" }
        };

        /// <summary>
        /// The mapping of supported ToUniversalTime() method translations.
        /// </summary>
        [NotNull] static readonly HashSet<MethodInfo> MethodInfoToUniversalTime = new HashSet<MethodInfo>
        {
            typeof(DateTime).GetRuntimeMethod(nameof(DateTime.ToUniversalTime), Type.EmptyTypes),
            // Supported for the sake of chaining with other members within a query, should never be directly SELECTed into .NET.
            //   It WILL result in a bogus value.
            typeof(DateTimeOffset).GetRuntimeMethod(nameof(DateTimeOffset.ToUniversalTime), Type.EmptyTypes)
        };

        readonly ISqlExpressionFactory _sqlExpressionFactory;
        readonly RelationalTypeMapping _intervalMapping;
        readonly RelationalTypeMapping _textMapping;

        /// <summary>
        /// Initializes a new instance of the <see cref="Npgsql.EntityFrameworkCore.PostgreSQL.Query.ExpressionTranslators.Internal.NpgsqlDateTimeMethodTranslator"/> class.
        /// </summary>
        public NpgsqlDateTimeMethodTranslator(
            [NotNull] IRelationalTypeMappingSource typeMappingSource,
            [NotNull] ISqlExpressionFactory sqlExpressionFactory)
        {
            _sqlExpressionFactory = sqlExpressionFactory;
            _intervalMapping = typeMappingSource.FindMapping("interval");
            _textMapping = typeMappingSource.FindMapping("text");
        }

        /// <inheritdoc />
        public virtual SqlExpression Translate(
                SqlExpression instance,
                MethodInfo method,
                IReadOnlyList<SqlExpression> arguments,
                IDiagnosticsLogger<DbLoggerCategory.Query> logger)
            => method switch
            {
                _ when MethodInfoDatePartMapping.TryGetValue(method, out var datePart)  => TranslateAddInterval(instance, arguments, datePart),
                _ when MethodInfoToUniversalTime.Contains(method)                       => TranslateToUniversalTime(instance, method.ReturnType),
                _                                                                       => null
            };

        private SqlExpression TranslateAddInterval(SqlExpression instance, IReadOnlyList<SqlExpression> arguments, string datePart)
        {
            var interval = arguments[0];

            if (instance is null || interval is null)
                return null;

            // Note: ideally we'd simply generate a PostgreSQL interval expression, but the .NET mapping of that is TimeSpan,
            // which does not work for months, years, etc. So we generate special fragments instead.
            if (interval is SqlConstantExpression constantExpression)
            {
                // We generate constant intervals as INTERVAL '1 days'
                if (constantExpression.Type == typeof(double) &&
                    ((double)constantExpression.Value >= int.MaxValue ||
                     (double)constantExpression.Value <= int.MinValue))
                {
                    return null;
                }

                interval = _sqlExpressionFactory.Fragment(FormattableString.Invariant($"INTERVAL '{constantExpression.Value} {datePart}'"));
            }
            else
            {
                // For non-constants, we can't parameterize INTERVAL '1 days'. Instead, we use CAST($1 || ' days' AS interval).
                // Note that a make_interval() function also exists, but accepts only int (for all fields except for
                // seconds), so we don't use it.
                // Note: we instantiate SqlBinaryExpression manually rather than via sqlExpressionFactory because
                // of the non-standard Add expression (concatenate int with text)
                interval = _sqlExpressionFactory.Convert(
                    new SqlBinaryExpression(
                        ExpressionType.Add,
                        _sqlExpressionFactory.Convert(interval, typeof(string), _textMapping),
                        _sqlExpressionFactory.Constant(' ' + datePart, _textMapping),
                        typeof(string),
                        _textMapping),
                    typeof(TimeSpan),
                    _intervalMapping);
            }

            return _sqlExpressionFactory.Add(instance, interval, instance.TypeMapping);
        }

        private SqlExpression TranslateToUniversalTime(SqlExpression instance, Type returnType)
            => (_sqlExpressionFactory is NpgsqlSqlExpressionFactory npgsqlSqlExpressionFactory)
                ? npgsqlSqlExpressionFactory.AtTimeZone(
                    instance,
                    _sqlExpressionFactory.Constant("UTC"),
                    returnType)
                : null;
    }
}
