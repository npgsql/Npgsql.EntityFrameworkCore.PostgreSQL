using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Npgsql.EntityFrameworkCore.PostgreSQL.Query.ExpressionTranslators.Internal
{
    /// <summary>
    /// Translates methods defined on <see cref="T:System.Convert"/> into PostgreSQL CAST expressions.
    /// </summary>
    public class NpgsqlConvertTranslator : IMethodCallTranslator
    {
        private static readonly Dictionary<string, string> TypeMapping = new()
        {
            [nameof(Convert.ToBoolean)]  = "bool",
            [nameof(Convert.ToByte)]     = "smallint",
            [nameof(Convert.ToDateTime)] = "timestamp with time zone",
            [nameof(Convert.ToDecimal)]  = "numeric",
            [nameof(Convert.ToDouble)]   = "double precision",
            [nameof(Convert.ToInt16)]    = "smallint",
            [nameof(Convert.ToInt32)]    = "int",
            [nameof(Convert.ToInt64)]    = "bigint",
            [nameof(Convert.ToString)]   = "text"
        };

        private static readonly List<Type> SupportedTypes = new()
        {
            typeof(bool),
            typeof(byte),
            typeof(DateTime),
            typeof(decimal),
            typeof(double),
            typeof(float),
            typeof(int),
            typeof(long),
            typeof(short),
            typeof(string)
        };

        private static readonly List<MethodInfo> SupportedMethods
            = TypeMapping.Keys
                .SelectMany(
                    t => typeof(Convert).GetTypeInfo().GetDeclaredMethods(t)
                        .Where(
                            m => m.GetParameters().Length == 1
                                 && SupportedTypes.Contains(m.GetParameters().First().ParameterType)))
                .ToList();

        private readonly ISqlExpressionFactory _sqlExpressionFactory;

        public NpgsqlConvertTranslator(ISqlExpressionFactory sqlExpressionFactory)
            => _sqlExpressionFactory = sqlExpressionFactory;

        public virtual SqlExpression? Translate(
            SqlExpression? instance,
            MethodInfo method,
            IReadOnlyList<SqlExpression> arguments,
            IDiagnosticsLogger<DbLoggerCategory.Query> logger)
            => SupportedMethods.Contains(method)
                ? _sqlExpressionFactory.Convert(arguments[0], method.ReturnType)
                : null;
    }
}
