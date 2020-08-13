using System;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Storage;
using NpgsqlTypes;

namespace Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal.Mapping
{
    public class NpgsqlMoneyTypeMapping : DecimalTypeMapping
    {
        public NpgsqlMoneyTypeMapping() : base("money", System.Data.DbType.Currency) {}

        protected NpgsqlMoneyTypeMapping(RelationalTypeMappingParameters parameters)
            : base(parameters)
        {
        }

        protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
            => new NpgsqlMoneyTypeMapping();

        protected override string GenerateNonNullSqlLiteral(object value)
            => base.GenerateNonNullSqlLiteral(value) + "::money";
    }
}
