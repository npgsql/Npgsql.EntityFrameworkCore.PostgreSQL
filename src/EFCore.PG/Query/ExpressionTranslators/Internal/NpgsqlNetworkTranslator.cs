using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Expressions.Internal;

namespace Npgsql.EntityFrameworkCore.PostgreSQL.Query.ExpressionTranslators.Internal
{
    /// <summary>
    /// Provides translation services for operators and functions of PostgreSQL network typess (cidr, inet, macaddr, macaddr8).
    /// </summary>
    /// <remarks>
    /// See: https://www.postgresql.org/docs/current/static/functions-net.html
    /// </remarks>
    public class NpgsqlNetworkTranslator : IMethodCallTranslator
    {
        [NotNull] static readonly MethodInfo IPAddressParse =
            typeof(IPAddress).GetRuntimeMethod(nameof(IPAddress.Parse), new[] { typeof(string) });

        [NotNull] static readonly MethodInfo PhysicalAddressParse =
            typeof(PhysicalAddress).GetRuntimeMethod(nameof(PhysicalAddress.Parse), new[] { typeof(string) });

        [NotNull]
        readonly ISqlExpressionFactory _sqlExpressionFactory;

        readonly RelationalTypeMapping _boolMapping;
        readonly RelationalTypeMapping _inetMapping;
        readonly RelationalTypeMapping _cidrMapping;
        readonly RelationalTypeMapping _macaddr8Mapping;

        public NpgsqlNetworkTranslator(ISqlExpressionFactory sqlExpressionFactory, IRelationalTypeMappingSource typeMappingSource)
        {
            _sqlExpressionFactory = sqlExpressionFactory;
            _boolMapping = typeMappingSource.FindMapping(typeof(bool));
            _inetMapping = typeMappingSource.FindMapping("inet");
            _cidrMapping = typeMappingSource.FindMapping("cidr");
            _macaddr8Mapping = typeMappingSource.FindMapping("macaddr8");
        }

        /// <inheritdoc />
        [CanBeNull]
        public SqlExpression Translate(SqlExpression instance, MethodInfo method, IReadOnlyList<SqlExpression> arguments)
        {
            if (method == IPAddressParse)
                return _sqlExpressionFactory.Convert(arguments[0], typeof(IPAddress), _sqlExpressionFactory.FindMapping(typeof(IPAddress)));

            if (method == PhysicalAddressParse)
                return _sqlExpressionFactory.Convert(arguments[0], typeof(PhysicalAddress), _sqlExpressionFactory.FindMapping(typeof(PhysicalAddress)));

            if (method.DeclaringType != typeof(NpgsqlNetworkExtensions))
                return null;

            return method.Name switch
            {
            nameof(NpgsqlNetworkExtensions.LessThan)              => _sqlExpressionFactory.LessThan(arguments[1], arguments[2]),
            nameof(NpgsqlNetworkExtensions.LessThanOrEqual)       => _sqlExpressionFactory.LessThanOrEqual(arguments[1], arguments[2]),
            nameof(NpgsqlNetworkExtensions.GreaterThanOrEqual)    => _sqlExpressionFactory.GreaterThanOrEqual(arguments[1], arguments[2]),
            nameof(NpgsqlNetworkExtensions.GreaterThan)           => _sqlExpressionFactory.GreaterThan(arguments[1], arguments[2]),

            nameof(NpgsqlNetworkExtensions.ContainedBy)           => BoolReturningOnTwoNetworkTypes("<<"),
            nameof(NpgsqlNetworkExtensions.ContainedByOrEqual)    => BoolReturningOnTwoNetworkTypes("<<="),
            nameof(NpgsqlNetworkExtensions.Contains)              => BoolReturningOnTwoNetworkTypes(">>"),
            nameof(NpgsqlNetworkExtensions.ContainsOrEqual)       => BoolReturningOnTwoNetworkTypes(">>="),
            nameof(NpgsqlNetworkExtensions.ContainsOrContainedBy) => BoolReturningOnTwoNetworkTypes("&&"),

            // TODO: Hack, see #1118
            nameof(NpgsqlNetworkExtensions.BitwiseNot)            => new SqlUnaryExpression(ExpressionType.Negate,
                arguments[1],
                arguments[1].Type,
                arguments[1].TypeMapping),

            nameof(NpgsqlNetworkExtensions.BitwiseAnd) => _sqlExpressionFactory.And(arguments[1], arguments[2]),
            nameof(NpgsqlNetworkExtensions.BitwiseOr)  => _sqlExpressionFactory.Or(arguments[1], arguments[2]),

            // Add/Subtract accept inet + int, so we can't use the default type mapping inference logic which assumes
            // same-typed operands
            nameof(NpgsqlNetworkExtensions.Add)
                => new SqlBinaryExpression(
                    ExpressionType.Add,
                    _sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[1]),
                    _sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[2]),
                    arguments[1].Type,
                    arguments[1].TypeMapping),

            nameof(NpgsqlNetworkExtensions.Subtract) when arguments[2].Type == typeof(int)
                => new SqlBinaryExpression(
                    ExpressionType.Subtract,
                    _sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[1]),
                    _sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[2]),
                    arguments[1].Type,
                    arguments[1].TypeMapping),

            nameof(NpgsqlNetworkExtensions.Subtract)
                when arguments[2].Type == typeof(IPAddress) || arguments[2].Type == typeof((IPAddress, int))
                => new SqlBinaryExpression(
                    ExpressionType.Subtract,
                    _sqlExpressionFactory.ApplyTypeMapping(arguments[1], ExpressionExtensions.InferTypeMapping(arguments[1], arguments[2])),
                    _sqlExpressionFactory.ApplyTypeMapping(arguments[2], ExpressionExtensions.InferTypeMapping(arguments[1], arguments[2])),
                    arguments[1].Type,
                    _sqlExpressionFactory.FindMapping(typeof(long))),

            nameof(NpgsqlNetworkExtensions.Abbreviate)    => _sqlExpressionFactory.Function("abbrev",           new[] { arguments[1] }, typeof(string)),
            nameof(NpgsqlNetworkExtensions.Broadcast)     => _sqlExpressionFactory.Function("broadcast",        new[] { arguments[1] }, typeof(IPAddress), _inetMapping),
            nameof(NpgsqlNetworkExtensions.Family)        => _sqlExpressionFactory.Function("family",           new[] { arguments[1] }, typeof(int)),
            nameof(NpgsqlNetworkExtensions.Host)          => _sqlExpressionFactory.Function("host",             new[] { arguments[1] }, typeof(string)),
            nameof(NpgsqlNetworkExtensions.HostMask)      => _sqlExpressionFactory.Function("hostmask",         new[] { arguments[1] }, typeof(IPAddress), _inetMapping),
            nameof(NpgsqlNetworkExtensions.MaskLength)    => _sqlExpressionFactory.Function("masklen",          new[] { arguments[1] }, typeof(int)),
            nameof(NpgsqlNetworkExtensions.Netmask)       => _sqlExpressionFactory.Function("netmask",          new[] { arguments[1] }, typeof(IPAddress), _inetMapping),
            nameof(NpgsqlNetworkExtensions.Network)       => _sqlExpressionFactory.Function("network",          new[] { arguments[1] }, typeof((IPAddress Address, int Subnet)), _cidrMapping),
            nameof(NpgsqlNetworkExtensions.SetMaskLength) => _sqlExpressionFactory.Function("set_masklen",      new[] { arguments[1], arguments[2] }, arguments[1].Type, arguments[1].TypeMapping),
            nameof(NpgsqlNetworkExtensions.Text)          => _sqlExpressionFactory.Function("text",             new[] { arguments[1] }, typeof(string)),
            nameof(NpgsqlNetworkExtensions.SameFamily)    => _sqlExpressionFactory.Function("inet_same_family", new[] { arguments[1], arguments[2] }, typeof(bool)),
            nameof(NpgsqlNetworkExtensions.Merge)         => _sqlExpressionFactory.Function("inet_merge",       new[] { arguments[1], arguments[2] }, typeof((IPAddress Address, int Subnet)), _cidrMapping),
            nameof(NpgsqlNetworkExtensions.Truncate)      => _sqlExpressionFactory.Function("trunc",            new[] { arguments[1] }, typeof(PhysicalAddress), arguments[1].TypeMapping),
            nameof(NpgsqlNetworkExtensions.Set7BitMac8)   => _sqlExpressionFactory.Function("macaddr8_set7bit", new[] { arguments[1] }, typeof(PhysicalAddress), _macaddr8Mapping),

            _ => (SqlExpression)null
            };

            SqlCustomBinaryExpression BoolReturningOnTwoNetworkTypes(string @operator)
                => new SqlCustomBinaryExpression(
                    _sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[1]),
                    _sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[2]),
                    @operator,
                    typeof(bool),
                    _boolMapping);
        }
    }
}
