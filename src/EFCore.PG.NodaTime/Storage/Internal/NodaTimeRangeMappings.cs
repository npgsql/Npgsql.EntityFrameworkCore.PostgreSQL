// ReSharper disable once CheckNamespace

using System;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime;
using NodaTime.Text;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal.Mapping;
using NpgsqlTypes;

#nullable enable

// ReSharper disable once CheckNamespace
namespace Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal
{

    public class DateIntervalMapping : NpgsqlTypeMapping
    {
        static readonly ConstructorInfo ConstructorWithDates =
            typeof(DateInterval).GetConstructor(new[] { typeof(LocalDate), typeof(LocalDate) })!;

        static readonly ConstructorInfo LocalDateConstructor =
            typeof(LocalDate).GetConstructor(new[] { typeof(int), typeof(int), typeof(int) })!;

        // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
        public DateIntervalMapping() : base("daterange", typeof(DateInterval), NpgsqlDbType.Range |  NpgsqlDbType.Date)
        {
        }

        // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
        protected DateIntervalMapping(RelationalTypeMappingParameters parameters) : base(parameters, NpgsqlDbType.Range |  NpgsqlDbType.Date)
        {
        }

        protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters) => new DateIntervalMapping(parameters);

        public override RelationalTypeMapping Clone(string storeType, int? size)
            => new DateIntervalMapping(Parameters.WithStoreTypeAndSize(storeType, size));

        public override CoreTypeMapping Clone(ValueConverter? converter)
            => new DateIntervalMapping(Parameters.WithComposedConverter(converter));

        protected override string GenerateNonNullSqlLiteral(object value)
        {
            var range = (DateInterval) value;
            return $"'[{LocalDatePattern.Iso.Format(range.Start)},  {LocalDatePattern.Iso.Format(range.End)}]'::daterange";
        }

        public override Expression GenerateCodeLiteral(object value)
        {
            var (start, end) = (DateInterval) value;
            return Expression.New(
                ConstructorWithDates,
                Expression.New(LocalDateConstructor, Expression.Constant(start.Year), Expression.Constant(start.Month), Expression.Constant(start.Day)),
                Expression.New(LocalDateConstructor, Expression.Constant(end.Year), Expression.Constant(end.Month), Expression.Constant(end.Day))
            );
        }

        public override DbParameter CreateParameter(DbCommand command, string name, object? value, bool? nullable = null)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            if (value is DateInterval range)
            {
                parameter.Value = new NpgsqlRange<LocalDate>(
                    range.Start,
                    true,
                    range.End,
                    true);
            }

            if (parameter is NpgsqlParameter postgresParameter)
            {
                // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
                postgresParameter.NpgsqlDbType = NpgsqlDbType.Range | NpgsqlDbType.Date;
            }

            return parameter;
        }

        public override Expression CustomizeDataReaderExpression(Expression expression)
        {
            Func<NpgsqlRange<LocalDate>, DateInterval> convertMethod = ConvertToNoda;
            var ret = Expression.Call(
                convertMethod.Method,
                expression
            );

            if (expression.Type.IsNullableValueType())
            {
                return Expression.Coalesce(
                    ret,
                    Expression.Constant(default(DateInterval))
                );
            }

            return ret;
        }

        private static DateInterval ConvertToNoda(NpgsqlRange<LocalDate> range)
        {
            var start = range.LowerBoundIsInclusive
                ? range.LowerBound
                : range.LowerBound.PlusDays(1);
            var end = range.UpperBoundIsInclusive
                ? range.UpperBound
                : range.UpperBound.PlusDays(-1);
            return new DateInterval(start, end);
        }
    }
}
