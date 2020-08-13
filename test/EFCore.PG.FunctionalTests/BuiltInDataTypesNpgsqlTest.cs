using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Net.NetworkInformation;
using Xunit;
using Xunit.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.TestUtilities;
using NpgsqlTypes;

namespace Npgsql.EntityFrameworkCore.PostgreSQL
{
    public class BuiltInDataTypesNpgsqlTest : BuiltInDataTypesTestBase<BuiltInDataTypesNpgsqlTest.BuiltInDataTypesNpgsqlFixture>
    {
        // ReSharper disable once UnusedParameter.Local
        public BuiltInDataTypesNpgsqlTest(BuiltInDataTypesNpgsqlFixture fixture, ITestOutputHelper testOutputHelper)
            : base(fixture)
        {
            Fixture.TestSqlLoggerFactory.Clear();
            //Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
        }

        [Fact]
        public void Sql_translation_uses_type_mapper_when_constant()
        {
            using var context = CreateContext();
            var results = context.Set<MappedNullableDataTypes>()
                .Where(e => e.TimeSpanAsTime == new TimeSpan(0, 1, 2))
                .Select(e => e.Int)
                .ToList();

            Assert.Empty(results);

            AssertSql(
                @"SELECT m.""Int""
FROM ""MappedNullableDataTypes"" AS m
WHERE m.""TimeSpanAsTime"" = TIME '00:01:02'");
        }

        [Fact]
        public void Sql_translation_uses_type_mapper_when_parameter()
        {
            using var context = CreateContext();
            var timeSpan = new TimeSpan(2, 1, 0);
            var results = context.Set<MappedNullableDataTypes>()
                .Where(e => e.TimeSpanAsTime == timeSpan)
                .Select(e => e.Int)
                .ToList();

            Assert.Empty(results);

            AssertSql(
                @"@__timeSpan_0='02:01:00' (Nullable = true) (DbType = Object)

SELECT m.""Int""
FROM ""MappedNullableDataTypes"" AS m
WHERE m.""TimeSpanAsTime"" = @__timeSpan_0");
        }

        [Fact]
        public virtual void Can_query_using_any_mapped_data_type()
        {
            using (var context = CreateContext())
            {
                context.Set<MappedNullableDataTypes>().Add(
                    new MappedNullableDataTypes
                    {
                        Int = 999,
                        LongAsBigint = 78L,
                        ShortAsSmallint = 79,
                        ByteAsSmallint = 80,
                        UintAsInt = uint.MaxValue,
                        UlongAsBigint = ulong.MaxValue,
                        UShortAsSmallint = ushort.MaxValue,
                        UintAsBigint = uint.MaxValue,
                        UShortAsInt = ushort.MaxValue,

                        BoolAsBoolean = true,

                        DecimalAsMoney = 81.1m,
                        Decimal = 101.7m,
                        DecimalAsNumeric = 103.9m,
                        FloatAsReal = 84.4f,
                        DoubleAsDoublePrecision = 85.5,

                        DateTimeAsTimestamp = new DateTime(2015, 1, 2, 10, 11, 12),
                        DateTimeAsTimestamptz = new DateTime(2016, 1, 2, 11, 11, 12, DateTimeKind.Utc),
                        DateTimeAsDate = new DateTime(2015, 1, 2, 0, 0, 0),
                        TimeSpanAsTime = new TimeSpan(11, 15, 12),
                        DateTimeOffsetAsTimetz = new DateTimeOffset(1, 1, 1, 12, 0, 0, TimeSpan.FromHours(2)),
                        TimeSpanAsInterval = new TimeSpan(11, 15, 12),

                        StringAsText = "Gumball Rules!",
                        StringAsVarchar = "Gumball Rules OK",
                        // TODO: enable here (and above) after https://github.com/aspnet/EntityFrameworkCore/issues/14159 is fixed
                        // CharAsChar1 = 'f',
                        CharAsText = 'g',
                        CharAsVarchar = 'h',
                        BytesAsBytea = new byte[] { 86 },

                        GuidAsUuid = new Guid("a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11"),

                        EnumAsText = StringEnum16.Value4,
                        EnumAsVarchar = StringEnumU16.Value4,

                        PhysicalAddressAsMacaddr = PhysicalAddress.Parse("08-00-2B-01-02-03"),
                        NpgsqlPointAsPoint = new NpgsqlPoint(5.2, 3.3),
                        StringAsJsonb = @"{""a"": ""b""}",
                        StringAsJson = @"{""a"": ""b""}",
                        DictionaryAsHstore = new Dictionary<string, string> { { "a", "b" } },
                        NpgsqlRangeAsRange = new NpgsqlRange<int>(4, true, 8, false),

                        IntArrayAsIntArray= new[] { 2, 3 },
                        PhysicalAddressArrayAsMacaddrArray= new[] { PhysicalAddress.Parse("08-00-2B-01-02-03"), PhysicalAddress.Parse("08-00-2B-01-02-04") },

                        UintAsXid = (uint)int.MaxValue + 1,

                        SearchQuery = NpgsqlTsQuery.Parse("a & b"),
                        SearchVector = NpgsqlTsVector.Parse("a b"),
                        RankingNormalization = NpgsqlTsRankingNormalization.DivideByLength,
                        Regconfig = 12724,

                        Mood = Mood.Sad
                    });

                Assert.Equal(1, context.SaveChanges());
            }

            using (var context = CreateContext())
            {
                var entity = context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999);

                long? param1 = 78L;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.LongAsBigint == param1));

                short? param2 = 79;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.ShortAsSmallint == param2));

                byte? param2a = 80;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.ByteAsSmallint == param2a));

                uint? param3 = uint.MaxValue;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.UintAsInt == param3));

                ulong? param4 = ulong.MaxValue;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.UlongAsBigint == param4));

                ushort? param5 = ushort.MaxValue;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.UShortAsSmallint == param5));

                uint? param6 = uint.MaxValue;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.UintAsBigint == param6));

                ushort? param7 = ushort.MaxValue;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.UShortAsInt == param7));

                bool? param8 = true;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.BoolAsBoolean == param8));

                // PostgreSQL doesn't support comparing money to decimal
                //decimal? param9 = 81.1m;
                //Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.DecimalAsMoney == param9));

                decimal? param10 = 101.7m;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.Decimal == param10));

                decimal? param11 = 103.9m;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.DecimalAsNumeric == param11));

                float? param12 = 84.4f;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.FloatAsReal == param12));

                double? param13 = 85.5;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.DoubleAsDoublePrecision == param13));

                DateTime? param14 = new DateTime(2015, 1, 2, 10, 11, 12);
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.DateTimeAsTimestamp == param14));

                DateTime? param15 = new DateTime(2016, 1, 2, 11, 11, 12, DateTimeKind.Utc);
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.DateTimeAsTimestamptz == param15));

                DateTime? param16 = new DateTime(2015, 1, 2, 0, 0, 0);
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.DateTimeAsDate == param16));

                TimeSpan? param17 = new TimeSpan(11, 15, 12);
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.TimeSpanAsTime == param17));

                DateTimeOffset? param18 = new DateTimeOffset(1, 1, 1, 12, 0, 0, TimeSpan.FromHours(2));
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.DateTimeOffsetAsTimetz == param18));

                TimeSpan? param19 = new TimeSpan(11, 15, 12);
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.TimeSpanAsInterval == param19));

                // ReSharper disable once ConvertToConstant.Local
                var param20 = "Gumball Rules!";
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.StringAsText == param20));

                // ReSharper disable once ConvertToConstant.Local
                var param21 = "Gumball Rules OK";
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.StringAsVarchar == param21));

                // TODO: enable here (and above) after https://github.com/aspnet/EntityFrameworkCore/issues/14159 is fixed
                // var param21a = 'f';
                // Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.CharAsChar1 == param21a));

                // ReSharper disable once ConvertToConstant.Local
                var param21b = 'g';
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.CharAsText == param21b));

                // ReSharper disable once ConvertToConstant.Local
                var param21c = 'h';
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.CharAsVarchar == param21c));

                var param22 = new byte[] { 86 };
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.BytesAsBytea == param22));

                Guid? param23 = new Guid("a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11");
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.GuidAsUuid == param23));

                StringEnum16? param24 = StringEnum16.Value4;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.EnumAsText == param24));

                StringEnumU16? param25 = StringEnumU16.Value4;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.EnumAsVarchar == param25));

                var param26 = PhysicalAddress.Parse("08-00-2B-01-02-03");
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.PhysicalAddressAsMacaddr.Equals(param26)));

                // PostgreSQL doesn't support equality comparison on point
                // NpgsqlPoint? param27 = new NpgsqlPoint(5.2, 3.3);
                // Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.Point == param27));

                // ReSharper disable once ConvertToConstant.Local
                var param28 = @"{""a"": ""b""}";
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.StringAsJsonb == param28));

                // operator does not exist: json = json (https://stackoverflow.com/questions/32843213/operator-does-not-exist-json-json)
                // var param29 = @"{""a"": ""b""}";
                // Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.StringAsJson == param29));

                var param30 = new Dictionary<string, string> { { "a", "b" } };
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.DictionaryAsHstore == param30));

                var param31 = new NpgsqlRange<int>(4, true, 8, false);
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.NpgsqlRangeAsRange == param31));

                var param32 = new[] { 2, 3 };
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.IntArrayAsIntArray == param32));

                var param33 = new[] { PhysicalAddress.Parse("08-00-2B-01-02-03"), PhysicalAddress.Parse("08-00-2B-01-02-04") };
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.PhysicalAddressArrayAsMacaddrArray == param33));

                // ReSharper disable once ConvertToConstant.Local
                var param34 = (uint)int.MaxValue + 1;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.UintAsXid == param34));

                var param35 = NpgsqlTsQuery.Parse("a & b");
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.SearchQuery == param35));

                var param36 = NpgsqlTsVector.Parse("a b");
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.SearchVector == param36));

                // ReSharper disable once ConvertToConstant.Local
                var param37 = NpgsqlTsRankingNormalization.DivideByLength;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.RankingNormalization == param37));

                // ReSharper disable once ConvertToConstant.Local
                var param38 = 12724u;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.Regconfig == param38));

                // ReSharper disable once ConvertToConstant.Local
                var param39 = Mood.Sad;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 999 && e.Mood == param39));
            }
        }


        [Fact]
        public virtual void Can_query_using_any_mapped_data_types_with_nulls()
        {
            using (var context = CreateContext())
            {
                context.Set<MappedNullableDataTypes>().Add(
                    new MappedNullableDataTypes
                    {
                        Int = 911,
                    });

                Assert.Equal(1, context.SaveChanges());
            }

            using (var context = CreateContext())
            {
                var entity = context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911);

                long? param1 = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.LongAsBigint == param1));

                short? param2 = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.ShortAsSmallint == param2));
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && (long?)(int?)e.ShortAsSmallint == param2));

                byte? param2a = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.ByteAsSmallint == param2a));
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && (long?)(int?)e.ByteAsSmallint == param2a));

                uint? param3 = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.UintAsInt == param3));

                ulong? param4 = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.UlongAsBigint == param4));

                ushort? param5 = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.UShortAsSmallint == param5));

                uint? param6 = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.UintAsBigint == param6));

                ushort? param7 = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.UShortAsInt == param7));

                bool? param8 = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.BoolAsBoolean == param8));

                decimal? param9 = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.DecimalAsMoney == param9));

                decimal? param10 = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.Decimal == param10));

                decimal? param11 = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.DecimalAsNumeric == param11));

                float? param12 = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.FloatAsReal == param12));

                double? param13 = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.DoubleAsDoublePrecision == param13));

                DateTime? param14 = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.DateTimeAsTimestamp == param14));

                DateTime? param15 = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.DateTimeAsTimestamptz == param15));

                DateTime? param16 = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.DateTimeAsDate == param16));

                TimeSpan? param17 = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.TimeSpanAsTime == param17));

                DateTimeOffset? param18 = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.DateTimeOffsetAsTimetz == param18));

                TimeSpan? param19 = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.TimeSpanAsInterval == param19));

                string param20 = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.StringAsText == param20));

                string param21 = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.StringAsVarchar == param21));

                // TODO: enable here (and above) after https://github.com/aspnet/EntityFrameworkCore/issues/14159 is fixed
                // char? param21a = null;
                // Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.CharAsChar1 == param21a));

                char? param21b = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.CharAsText == param21b));

                char? param21c = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.CharAsVarchar == param21c));

                byte[] param22 = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.BytesAsBytea == param22));

                Guid? param23 = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.GuidAsUuid == param23));

                StringEnum16? param24 = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.EnumAsText == param24));

                StringEnumU16? param25 = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.EnumAsVarchar == param25));

                PhysicalAddress param26 = null;
                // ReSharper disable once PossibleUnintendedReferenceComparison
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.PhysicalAddressAsMacaddr == param26));

                // PostgreSQL does not support equality comparison on geometry types, see https://www.postgresql.org/docs/current/functions-geometry.html
                //NpgsqlPoint? param27 = null;
                //Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.NpgsqlPointAsPoint == param27));

                string param28 = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.StringAsJsonb == param28));

                // PostgreSQL does not support equality comparison on json
                //string param29 = null;
                //Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.StringAsJson == param29));

                Dictionary<string, string> param30 = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.DictionaryAsHstore == param30));

                NpgsqlRange<int>? param31 = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.NpgsqlRangeAsRange == param31));

                int[] param32 = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.IntArrayAsIntArray == param32));

                PhysicalAddress[] param33 = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.PhysicalAddressArrayAsMacaddrArray== param33));

                uint? param34 = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.UintAsXid == param34));

                NpgsqlTsQuery param35 = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.SearchQuery == param35));

                NpgsqlTsVector param36 = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.SearchVector == param36));

                NpgsqlTsRankingNormalization? param37 = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.RankingNormalization == param37));

                uint? param38 = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.Regconfig == param38));

                Mood? param39 = null;
                Assert.Same(entity, context.Set<MappedNullableDataTypes>().Single(e => e.Int == 911 && e.Mood == param39));
            }
        }

        [Fact]
        public virtual void Can_insert_and_read_back_all_mapped_data_types()
        {
            var entity = CreateMappedDataTypes(77);
            using var context = CreateContext();
            context.Set<MappedDataTypes>().Add(entity);

            Assert.Equal(1, context.SaveChanges());

            var parameters = DumpParameters();
            Assert.Equal(
                @"@p0='77'
@p1='True'
@p2='80'
@p3='0x56' (Nullable = false)
@p4='g' (Nullable = false)
@p5='h' (Nullable = false)
@p6='2015-01-02T00:00:00' (DbType = Date)
@p7='2015-01-02T10:11:12' (DbType = DateTime)
@p8='2016-01-02T11:11:12' (DbType = DateTimeOffset)
@p9='0001-01-01T12:00:00.0000000+02:00' (DbType = Object)
@p10='101.7'
@p11='81.1' (DbType = Currency)
@p12='103.9'
@p13='System.Collections.Generic.Dictionary`2[System.String,System.String]' (Nullable = false) (DbType = Object)
@p14='85.5'
@p15='Value4' (Nullable = false)
@p16='Value4' (Nullable = false)
@p17='84.4'
@p18='a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11'
@p19='System.Int32[]' (Nullable = false) (DbType = Object)
@p20='78'
@p21='Sad' (DbType = Object)
@p22='(5.2,3.3)' (DbType = Object)
@p23='[4,8)' (DbType = Object)
@p24='System.Net.NetworkInformation.PhysicalAddress[]' (Nullable = false) (DbType = Object)
@p25='08002B010203' (Nullable = false) (DbType = Object)
@p26='2'
@p27='12724' (DbType = Object)
@p28=''a' & 'b'' (Nullable = false) (DbType = Object)
@p29=''a' 'b'' (Nullable = false) (DbType = Object)
@p30='79'
@p31='{""a"": ""b""}' (Nullable = false) (DbType = Object)
@p32='{""a"": ""b""}' (Nullable = false) (DbType = Object)
@p33='Gumball Rules!' (Nullable = false)
@p34='Gumball Rules OK' (Nullable = false)
@p35='11:15:12' (DbType = Object)
@p36='11:15:12' (DbType = Object)
@p37='65535'
@p38='-1'
@p39='4294967295'
@p40='-1'
@p41='2147483648' (DbType = Object)
@p42='-1'",
                    parameters,
                    ignoreLineEndingDifferences: true);
        }

        string DumpParameters()
            => Fixture.TestSqlLoggerFactory.Parameters.Single().Replace(", ", Environment.NewLine);

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        static void AssertMappedDataTypes(MappedDataTypes entity, int id)
        {
            // ReSharper disable once UnusedVariable
            var expected = CreateMappedDataTypes(id);
            Assert.Equal(id, entity.Int);
            Assert.Equal(78, entity.LongAsBigint);
            Assert.Equal(79, entity.ShortAsSmallint);
            Assert.Equal(uint.MaxValue, entity.UintAsInt);
            Assert.Equal(ulong.MaxValue, entity.UlongAsBigint);
            Assert.Equal(ushort.MaxValue, entity.UShortAsSmallint);
            Assert.Equal(uint.MaxValue, entity.UintAsBigint);
            Assert.Equal(ushort.MaxValue, entity.UShortAsInt);

            Assert.True(entity.BoolAsBoolean);

            Assert.Equal(81.1m, entity.DecimalAsMoney);
            Assert.Equal(101.7m, entity.Decimal);
            Assert.Equal(103.9m, entity.DecimalAsNumeric);
            Assert.Equal(84.4f, entity.FloatAsReal);
            Assert.Equal(85.5, entity.DoubleAsDoublePrecision);

            Assert.Equal(new DateTime(2015, 1, 2, 10, 11, 12), entity.DateTimeAsTimestamp);
            Assert.Equal(new DateTime(2016, 1, 2, 11, 11, 12, DateTimeKind.Utc), entity.DateTimeAsTimestamptz);
            Assert.Equal(new DateTime(2015, 1, 2, 0, 0, 0), entity.DateTimeAsDate);
            Assert.Equal(new TimeSpan(11, 15, 12), entity.TimeSpanAsTime);
            Assert.Equal(new DateTimeOffset(1, 1, 1, 12, 0, 0, TimeSpan.FromHours(2)), entity.DateTimeOffsetAsTimetz);
            Assert.Equal(new TimeSpan(11, 15, 12), entity.TimeSpanAsInterval);

            Assert.Equal("Gumball Rules!", entity.StringAsText);
            Assert.Equal("Gumball Rules OK", entity.StringAsVarchar);
            // TODO: enable here (and above) after https://github.com/aspnet/EntityFrameworkCore/issues/14159 is fixed
            // Assert.Equal('f', entity.CharAsChar1);
            Assert.Equal('g', entity.CharAsText);
            Assert.Equal('h', entity.CharAsVarchar);
            Assert.Equal(new byte[] { 86 }, entity.BytesAsBytea);

            Assert.Equal(new Guid("a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11"), entity.GuidAsUuid);

            Assert.Equal(StringEnum16.Value4, entity.EnumAsText);
            Assert.Equal(StringEnumU16.Value4, entity.EnumAsVarchar);

            Assert.Equal(PhysicalAddress.Parse("08-00-2B-01-02-03"), entity.PhysicalAddressAsMacaddr);
            Assert.Equal(new NpgsqlPoint(5.2, 3.3), entity.NpgsqlPointAsPoint);
            Assert.Equal(@"{""a"": ""b""}", entity.StringAsJsonb);
            Assert.Equal(@"{""a"": ""b""}", entity.StringAsJson);
            Assert.Equal(new Dictionary<string, string> { { "a", "b" } }, entity.DictionaryAsHstore);
            Assert.Equal(new NpgsqlRange<int>(4, true, 8, false), entity.NpgsqlRangeAsRange);

            Assert.Equal(new[] { 2, 3 }, entity.IntArrayAsIntArray);
            Assert.Equal(new[] { PhysicalAddress.Parse("08-00-2B-01-02-03"), PhysicalAddress.Parse("08-00-2B-01-02-04") }, entity.PhysicalAddressArrayAsMacaddrArray);

            Assert.Equal((uint)int.MaxValue + 1, entity.UintAsXid);

            Assert.Equal(NpgsqlTsQuery.Parse("a & b").ToString(), entity.SearchQuery.ToString());
            Assert.Equal(NpgsqlTsVector.Parse("a b").ToString(), entity.SearchVector.ToString());
            Assert.Equal(NpgsqlTsRankingNormalization.DivideByLength, entity.RankingNormalization);
        }

        static MappedDataTypes CreateMappedDataTypes(int id)
            => new MappedDataTypes
            {
                Int = id,
                LongAsBigint = 78L,
                ShortAsSmallint = 79,
                ByteAsSmallint = 80,
                UintAsInt = uint.MaxValue,
                UlongAsBigint = ulong.MaxValue,
                UShortAsSmallint = ushort.MaxValue,
                UintAsBigint = uint.MaxValue,
                UShortAsInt = ushort.MaxValue,

                BoolAsBoolean = true,

                DecimalAsMoney = 81.1m,
                Decimal = 101.7m,
                DecimalAsNumeric = 103.9m,
                FloatAsReal = 84.4f,
                DoubleAsDoublePrecision = 85.5,

                DateTimeAsTimestamp = new DateTime(2015, 1, 2, 10, 11, 12),
                DateTimeAsTimestamptz = new DateTime(2016, 1, 2, 11, 11, 12, DateTimeKind.Utc),
                DateTimeAsDate = new DateTime(2015, 1, 2, 0, 0, 0),
                TimeSpanAsTime = new TimeSpan(11, 15, 12),
                DateTimeOffsetAsTimetz = new DateTimeOffset(1, 1, 1, 12, 0, 0, TimeSpan.FromHours(2)),
                TimeSpanAsInterval = new TimeSpan(11, 15, 12),

                StringAsText = "Gumball Rules!",
                StringAsVarchar = "Gumball Rules OK",
                // TODO: enable here (and above) after https://github.com/aspnet/EntityFrameworkCore/issues/14159 is fixed
                // CharAsChar1 = 'f',
                CharAsText = 'g',
                CharAsVarchar = 'h',
                BytesAsBytea = new byte[] { 86 },

                GuidAsUuid = new Guid("a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11"),

                EnumAsText = StringEnum16.Value4,
                EnumAsVarchar = StringEnumU16.Value4,

                PhysicalAddressAsMacaddr = PhysicalAddress.Parse("08-00-2B-01-02-03"),
                NpgsqlPointAsPoint = new NpgsqlPoint(5.2, 3.3),
                StringAsJsonb = @"{""a"": ""b""}",
                StringAsJson = @"{""a"": ""b""}",
                DictionaryAsHstore = new Dictionary<string, string> { { "a", "b" } },
                NpgsqlRangeAsRange = new NpgsqlRange<int>(4, true, 8, false),

                IntArrayAsIntArray = new[] { 2, 3 },
                PhysicalAddressArrayAsMacaddrArray = new[] { PhysicalAddress.Parse("08-00-2B-01-02-03"), PhysicalAddress.Parse("08-00-2B-01-02-04") },

                UintAsXid = (uint)int.MaxValue + 1,

                SearchQuery = NpgsqlTsQuery.Parse("a & b"),
                SearchVector = NpgsqlTsVector.Parse("a b"),
                RankingNormalization = NpgsqlTsRankingNormalization.DivideByLength,
                Regconfig = 12724,

                Mood = Mood.Sad
            };

        [Fact]
        public virtual void Can_insert_and_read_back_all_mapped_data_types_set_to_null()
        {
            using (var context = CreateContext())
            {
                context.Set<MappedNullableDataTypes>().Add(new MappedNullableDataTypes { Int = 78 });

                Assert.Equal(1, context.SaveChanges());
            }

            using (var context = CreateContext())
            {
                AssertNullMappedNullableDataTypes(context.Set<MappedNullableDataTypes>().Single(e => e.Int == 78), 78);
            }
        }

        // ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local
        static void AssertNullMappedNullableDataTypes(MappedNullableDataTypes entity, int id)
        // ReSharper restore ParameterOnlyUsedForPreconditionCheck.Local
        {
            Assert.Equal(id, entity.Int);
            Assert.Null(entity.LongAsBigint);
            Assert.Null(entity.ShortAsSmallint);
            Assert.Null(entity.ByteAsSmallint);
            Assert.Null(entity.UintAsInt);
            Assert.Null(entity.UlongAsBigint);
            Assert.Null(entity.UShortAsSmallint);
            Assert.Null(entity.UintAsBigint);
            Assert.Null(entity.UShortAsInt);

            Assert.Null(entity.BoolAsBoolean);

            Assert.Null(entity.DecimalAsMoney);
            Assert.Null(entity.Decimal);
            Assert.Null(entity.DecimalAsNumeric);
            Assert.Null(entity.FloatAsReal);
            Assert.Null(entity.DoubleAsDoublePrecision);

            Assert.Null(entity.DateTimeAsTimestamp);
            Assert.Null(entity.DateTimeAsTimestamptz);
            Assert.Null(entity.DateTimeAsDate);
            Assert.Null(entity.TimeSpanAsTime);
            Assert.Null(entity.DateTimeOffsetAsTimetz);
            Assert.Null(entity.TimeSpanAsInterval);

            Assert.Null(entity.StringAsText);
            Assert.Null(entity.StringAsVarchar);
            // TODO: enable here (and above) after https://github.com/aspnet/EntityFrameworkCore/issues/14159 is fixed
            // Assert.Null(entity.CharAsChar1);
            Assert.Null(entity.CharAsText);
            Assert.Null(entity.CharAsVarchar);
            Assert.Null(entity.BytesAsBytea);

            Assert.Null(entity.GuidAsUuid);

            Assert.Null(entity.EnumAsText);
            Assert.Null(entity.EnumAsVarchar);

            Assert.Null(entity.PhysicalAddressAsMacaddr);
            Assert.Null(entity.NpgsqlPointAsPoint);
            Assert.Null(entity.StringAsJsonb);
            Assert.Null(entity.StringAsJson);
            Assert.Null(entity.DictionaryAsHstore);
            Assert.Null(entity.NpgsqlRangeAsRange);

            Assert.Null(entity.IntArrayAsIntArray);
            Assert.Null(entity.PhysicalAddressArrayAsMacaddrArray);

            Assert.Null(entity.UintAsXid);

            Assert.Null(entity.SearchQuery);
            Assert.Null(entity.SearchVector);
            Assert.Null(entity.RankingNormalization);

            Assert.Null(entity.Mood);
        }

        [Fact(Skip="https://github.com/aspnet/EntityFrameworkCore/issues/14159")]
        public override void Can_query_using_any_data_type_nullable_shadow() {}

        [Fact(Skip="https://github.com/aspnet/EntityFrameworkCore/issues/14159")]
        public override void Can_query_using_any_data_type_shadow() {}

        public override void Can_query_with_null_parameters_using_any_nullable_data_type()
        {
            using (var context = CreateContext())
            {
                context.Set<BuiltInNullableDataTypes>().Add(
                    new BuiltInNullableDataTypes
                    {
                        Id = 711
                    });

                Assert.Equal(1, context.SaveChanges());
            }

            using (var context = CreateContext())
            {
                var entity = context.Set<BuiltInNullableDataTypes>().Where(e => e.Id == 711).ToList().Single();

                short? param1 = null;
                Assert.Same(
                    entity,
                    context.Set<BuiltInNullableDataTypes>().Where(e => e.Id == 711 && e.TestNullableInt16 == param1).ToList().Single());
                Assert.Same(
                    entity,
                    context.Set<BuiltInNullableDataTypes>().Where(e => e.Id == 711 && (long?)(int?)e.TestNullableInt16 == param1).ToList()
                        .Single());

                int? param2 = null;
                Assert.Same(
                    entity,
                    context.Set<BuiltInNullableDataTypes>().Where(e => e.Id == 711 && e.TestNullableInt32 == param2).ToList().Single());

                long? param3 = null;
                Assert.Same(
                    entity,
                    context.Set<BuiltInNullableDataTypes>().Where(e => e.Id == 711 && e.TestNullableInt64 == param3).ToList().Single());

                double? param4 = null;
                Assert.Same(
                    entity,
                    context.Set<BuiltInNullableDataTypes>().Where(e => e.Id == 711 && e.TestNullableDouble == param4).ToList().Single());

                decimal? param5 = null;
                Assert.Same(
                    entity,
                    context.Set<BuiltInNullableDataTypes>().Where(e => e.Id == 711 && e.TestNullableDecimal == param5).ToList().Single());

                DateTime? param6 = null;
                Assert.Same(
                    entity,
                    context.Set<BuiltInNullableDataTypes>().Where(e => e.Id == 711 && e.TestNullableDateTime == param6).ToList().Single());

                // We don't support DateTimeOffset

                TimeSpan? param8 = null;
                Assert.Same(
                    entity,
                    context.Set<BuiltInNullableDataTypes>().Where(e => e.Id == 711 && e.TestNullableTimeSpan == param8).ToList().Single());

                float? param9 = null;
                Assert.Same(
                    entity,
                    context.Set<BuiltInNullableDataTypes>().Where(e => e.Id == 711 && e.TestNullableSingle == param9).ToList().Single());

                bool? param10 = null;
                Assert.Same(
                    entity,
                    context.Set<BuiltInNullableDataTypes>().Where(e => e.Id == 711 && e.TestNullableBoolean == param10).ToList().Single());

                // We don't support byte

                Enum64? param12 = null;
                Assert.Same(
                    entity, context.Set<BuiltInNullableDataTypes>().Where(e => e.Id == 711 && e.Enum64 == param12).ToList().Single());

                Enum32? param13 = null;
                Assert.Same(
                    entity, context.Set<BuiltInNullableDataTypes>().Where(e => e.Id == 711 && e.Enum32 == param13).ToList().Single());

                Enum16? param14 = null;
                Assert.Same(
                    entity, context.Set<BuiltInNullableDataTypes>().Where(e => e.Id == 711 && e.Enum16 == param14).ToList().Single());

                Enum8? param15 = null;
                Assert.Same(
                    entity, context.Set<BuiltInNullableDataTypes>().Where(e => e.Id == 711 && e.Enum8 == param15).ToList().Single());

                var entityType = context.Model.FindEntityType(typeof(BuiltInNullableDataTypes));
                if (entityType.FindProperty(nameof(BuiltInNullableDataTypes.TestNullableUnsignedInt16)) != null)
                {
                    ushort? param16 = null;
                    Assert.Same(
                        entity,
                        context.Set<BuiltInNullableDataTypes>().Where(e => e.Id == 711 && e.TestNullableUnsignedInt16 == param16).ToList()
                            .Single());
                }

                if (entityType.FindProperty(nameof(BuiltInNullableDataTypes.TestNullableUnsignedInt32)) != null)
                {
                    uint? param17 = null;
                    Assert.Same(
                        entity,
                        context.Set<BuiltInNullableDataTypes>().Where(e => e.Id == 711 && e.TestNullableUnsignedInt32 == param17).ToList()
                            .Single());
                }

                if (entityType.FindProperty(nameof(BuiltInNullableDataTypes.TestNullableUnsignedInt64)) != null)
                {
                    ulong? param18 = null;
                    Assert.Same(
                        entity,
                        context.Set<BuiltInNullableDataTypes>().Where(e => e.Id == 711 && e.TestNullableUnsignedInt64 == param18).ToList()
                            .Single());
                }

                if (entityType.FindProperty(nameof(BuiltInNullableDataTypes.TestNullableCharacter)) != null)
                {
                    char? param19 = null;
                    Assert.Same(
                        entity,
                        context.Set<BuiltInNullableDataTypes>().Where(e => e.Id == 711 && e.TestNullableCharacter == param19).ToList()
                            .Single());
                }

                if (entityType.FindProperty(nameof(BuiltInNullableDataTypes.TestNullableSignedByte)) != null)
                {
                    sbyte? param20 = null;
                    Assert.Same(
                        entity,
                        context.Set<BuiltInNullableDataTypes>().Where(e => e.Id == 711 && e.TestNullableSignedByte == param20).ToList()
                            .Single());
                }

                if (entityType.FindProperty(nameof(BuiltInNullableDataTypes.EnumU64)) != null)
                {
                    EnumU64? param21 = null;
                    Assert.Same(
                        entity, context.Set<BuiltInNullableDataTypes>().Where(e => e.Id == 711 && e.EnumU64 == param21).ToList().Single());
                }

                if (entityType.FindProperty(nameof(BuiltInNullableDataTypes.EnumU32)) != null)
                {
                    EnumU32? param22 = null;
                    Assert.Same(
                        entity, context.Set<BuiltInNullableDataTypes>().Where(e => e.Id == 711 && e.EnumU32 == param22).ToList().Single());
                }

                if (entityType.FindProperty(nameof(BuiltInNullableDataTypes.EnumU16)) != null)
                {
                    EnumU16? param23 = null;
                    Assert.Same(
                        entity, context.Set<BuiltInNullableDataTypes>().Where(e => e.Id == 711 && e.EnumU16 == param23).ToList().Single());
                }

                if (entityType.FindProperty(nameof(BuiltInNullableDataTypes.EnumS8)) != null)
                {
                    EnumS8? param24 = null;
                    Assert.Same(
                        entity, context.Set<BuiltInNullableDataTypes>().Where(e => e.Id == 711 && e.EnumS8 == param24).ToList().Single());
                }
            }
        }

        [ConditionalFact]
        public void Sum_Conversions()
        {
            using var context = CreateContext();

            // PostgreSQL SUM() returns numeric for bigint input, bigint for int/smallint ints.
            // Make sure the proper conversion is done
            var sum1 = context.Set<MappedDataTypes>().Sum(m => m.LongAsBigint);
            var sum2 = context.Set<MappedDataTypes>().Sum(m => m.Int);
            var sum3 = context.Set<MappedDataTypes>().Sum(m => m.ShortAsSmallint);

            AssertSql(
                @"SELECT SUM(m.""LongAsBigint"")::bigint
FROM ""MappedDataTypes"" AS m",
                //
                @"SELECT SUM(m.""Int"")::INT
FROM ""MappedDataTypes"" AS m",
                //
                @"SELECT SUM(CAST(m.""ShortAsSmallint"" AS integer))::INT
FROM ""MappedDataTypes"" AS m");
        }

        [ConditionalFact]
        public void Money_compare_constant()
        {
            using var context = CreateContext();

            _ = context.Set<MappedDataTypes>().Where(m => m.DecimalAsMoney > 3).ToList();
        }

        [ConditionalFact]
        public void Money_compare_parameter()
        {
            using var context = CreateContext();

            var money = 3m;
            _ = context.Set<MappedDataTypes>().Where(m => m.DecimalAsMoney > money).ToList();
        }

        void AssertSql(params string[] expected)
            => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);

        public class BuiltInDataTypesNpgsqlFixture : BuiltInDataTypesFixtureBase
        {
            public override bool StrictEquality => false;

            public override bool SupportsAnsi => false;

            public override bool SupportsUnicodeToAnsiConversion => false;

            public override bool SupportsLargeStringComparisons => true;

            public override bool SupportsDecimalComparisons => true;

            protected override ITestStoreFactory TestStoreFactory => NpgsqlTestStoreFactory.Instance;

            protected override bool ShouldLogCategory(string logCategory)
                => logCategory == DbLoggerCategory.Query.Name;

            public TestSqlLoggerFactory TestSqlLoggerFactory => (TestSqlLoggerFactory)ServiceProvider.GetRequiredService<ILoggerFactory>();

            protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
            {
                base.OnModelCreating(modelBuilder, context);

                NpgsqlConnection.GlobalTypeMapper.MapEnum<Mood>();
                ((NpgsqlTypeMappingSource)context.GetService<ITypeMappingSource>()).LoadUserDefinedTypeMappings(context.GetService<ISqlGenerationHelper>());

                modelBuilder.HasPostgresExtension("hstore");
                modelBuilder.HasPostgresEnum("mood", new[] { "happy", "sad" });

                MakeRequired<MappedDataTypes>(modelBuilder);

                modelBuilder.Entity<BuiltInDataTypes>(b =>
                {
                    b.Ignore(dt => dt.TestUnsignedInt16);
                    b.Ignore(dt => dt.TestUnsignedInt32);
                    b.Ignore(dt => dt.TestUnsignedInt64);
                    b.Ignore(dt => dt.TestCharacter);
                    b.Ignore(dt => dt.TestSignedByte);
                    b.Ignore(dt => dt.TestDateTimeOffset);
                    b.Ignore(dt => dt.TestByte);
                    //b.Ignore(dt => dt.EnumU16);
                    //b.Ignore(dt => dt.EnumU32);
                    //b.Ignore(dt => dt.EnumU64);
                    //b.Ignore(dt => dt.EnumS8);
                });

                modelBuilder.Entity<BuiltInNullableDataTypes>(b =>
                {
                    b.Ignore(dt => dt.TestNullableUnsignedInt16);
                    b.Ignore(dt => dt.TestNullableUnsignedInt32);
                    b.Ignore(dt => dt.TestNullableUnsignedInt64);
                    b.Ignore(dt => dt.TestNullableCharacter);
                    b.Ignore(dt => dt.TestNullableSignedByte);
                    b.Ignore(dt => dt.TestNullableDateTimeOffset);
                    b.Ignore(dt => dt.TestNullableByte);
                    //b.Ignore(dt => dt.EnumU16);
                    //b.Ignore(dt => dt.EnumU32);
                    //b.Ignore(dt => dt.EnumU64);
                    //b.Ignore(dt => dt.EnumS8);
                });

                modelBuilder.Entity<MappedDataTypes>(
                    b =>
                    {
                        b.HasKey(e => e.Int);
                        b.Property(e => e.Int).ValueGeneratedNever();
                    });

                modelBuilder.Entity<MappedNullableDataTypes>(
                    b =>
                    {
                        b.HasKey(e => e.Int);
                        b.Property(e => e.Int).ValueGeneratedNever();
                    });

                modelBuilder.Entity<MappedSizedDataTypes>()
                    .Property(e => e.Id)
                    .ValueGeneratedNever();

                modelBuilder.Entity<MappedScaledDataTypes>()
                    .Property(e => e.Id)
                    .ValueGeneratedNever();

                modelBuilder.Entity<MappedPrecisionAndScaledDataTypes>()
                    .Property(e => e.Id)
                    .ValueGeneratedNever();

                // Full text
                modelBuilder.Entity<MappedDataTypes>().Property(x => x.SearchQuery).HasColumnType("tsquery");
                modelBuilder.Entity<MappedDataTypes>().Property(x => x.SearchVector).HasColumnType("tsvector");
                modelBuilder.Entity<MappedDataTypes>().Property(x => x.RankingNormalization).HasColumnType("integer");
                modelBuilder.Entity<MappedDataTypes>().Property(x => x.Regconfig).HasColumnType("regconfig");
                modelBuilder.Entity<MappedNullableDataTypes>().Property(x => x.SearchQuery).HasColumnType("tsquery");
                modelBuilder.Entity<MappedNullableDataTypes>().Property(x => x.SearchVector).HasColumnType("tsvector");
                modelBuilder.Entity<MappedNullableDataTypes>().Property(x => x.RankingNormalization).HasColumnType("integer");
                modelBuilder.Entity<MappedNullableDataTypes>().Property(x => x.Regconfig).HasColumnType("regconfig");
            }

            public override bool SupportsBinaryKeys => true;

            public override DateTime DefaultDateTime => new DateTime();
        }

        protected enum StringEnum16 : short
        {
            // ReSharper disable once UnusedMember.Global
            Value1 = 1,
            // ReSharper disable once UnusedMember.Global
            Value2 = 2,
            Value4 = 4
        }

        protected enum StringEnumU16 : ushort
        {
            // ReSharper disable once UnusedMember.Global
            Value1 = 1,
            // ReSharper disable once UnusedMember.Global
            Value2 = 2,
            Value4 = 4
        }

        // ReSharper disable once MemberCanBePrivate.Global
        protected class MappedDataTypes
        {
            [Column(TypeName = "int")]
            public int Int { get; set; }

            [Column(TypeName = "bigint")]
            public long LongAsBigint { get; set; }

            [Column(TypeName = "smallint")]
            public short ShortAsSmallint { get; set; }

            [Column(TypeName = "smallint")]
            public byte ByteAsSmallint { get; set; }

            [Column(TypeName = "int")]
            public uint UintAsInt { get; set; }

            [Column(TypeName = "bigint")]
            public uint UintAsBigint { get; set; }

            [Column(TypeName = "bigint")]
            public ulong UlongAsBigint { get; set; }

            [Column(TypeName = "smallint")]
            public ushort UShortAsSmallint { get; set; }

            [Column(TypeName = "int")]
            public ushort UShortAsInt { get; set; }

            //[Column(TypeName = "tinyint")]
            //public sbyte SByteAsTinyint { get; set; }

            [Column(TypeName = "boolean")]
            public bool BoolAsBoolean { get; set; }

            [Column(TypeName = "numeric")]
            public decimal Decimal { get; set; }  // decimal is just an alias for numeric

            [Column(TypeName = "numeric")]
            public decimal DecimalAsNumeric { get; set; }

            [Column(TypeName = "money")]
            public decimal DecimalAsMoney { get; set; }

            [Column(TypeName = "double precision")]
            public double DoubleAsDoublePrecision { get; set; }

            [Column(TypeName = "real")]
            public float FloatAsReal { get; set; }

            [Column(TypeName = "timestamp")]
            public DateTime DateTimeAsTimestamp { get; set; }

            [Column(TypeName = "timestamptz")]
            public DateTime DateTimeAsTimestamptz { get; set; }

            [Column(TypeName = "date")]
            public DateTime DateTimeAsDate { get; set; }

            [Column(TypeName = "time")]
            public TimeSpan TimeSpanAsTime { get; set; }

            [Column(TypeName = "timetz")]
            public DateTimeOffset DateTimeOffsetAsTimetz { get; set; }

            [Column(TypeName = "interval")]
            public TimeSpan TimeSpanAsInterval { get; set; }

            [Column(TypeName = "text")]
            public string StringAsText { get; set; }

            [Column(TypeName = "varchar")]
            public string StringAsVarchar { get; set; }

            // TODO: enable here (and above) after https://github.com/aspnet/EntityFrameworkCore/issues/14159 is fixed
            // [Column(TypeName = "char(1)")]
            // public char? CharAsChar1 { get; set; }

            [Column(TypeName = "text")]
            public char? CharAsText { get; set; }

            [Column(TypeName = "varchar")]
            public char? CharAsVarchar { get; set; }

            [Column(TypeName = "bytea")]
            public byte[] BytesAsBytea { get; set; }

            [Column(TypeName = "uuid")]
            public Guid GuidAsUuid { get; set; }

            [Column(TypeName = "text")]
            public StringEnum16 EnumAsText { get; set; }

            [Column(TypeName = "varchar")]
            public StringEnumU16 EnumAsVarchar { get; set; }

            // PostgreSQL-specific types from here

            [Column(TypeName = "macaddr")]
            public PhysicalAddress PhysicalAddressAsMacaddr { get; set; }

            [Column(TypeName = "point")]
            public NpgsqlPoint NpgsqlPointAsPoint { get; set; }

            [Column(TypeName = "jsonb")]
            public string StringAsJsonb { get; set; }

            [Column(TypeName = "json")]
            public string StringAsJson { get; set; }

            [Column(TypeName = "hstore")]
            public Dictionary<string, string> DictionaryAsHstore { get; set; }

            [Column(TypeName = "int4range")]
            public NpgsqlRange<int> NpgsqlRangeAsRange { get; set; }

            [Column(TypeName = "int[]")]
            public int[] IntArrayAsIntArray { get; set; }

            [Column(TypeName = "macaddr[]")]
            public PhysicalAddress[] PhysicalAddressArrayAsMacaddrArray { get; set; }

            [Column(TypeName = "xid")]
            public uint UintAsXid { get; set; }

            public NpgsqlTsQuery SearchQuery { get; set; }
            public NpgsqlTsVector SearchVector { get; set; }
            public NpgsqlTsRankingNormalization RankingNormalization { get; set; }
            public uint Regconfig { get; set; }

            // ReSharper disable once UnusedAutoPropertyAccessor.Global
            [Column(TypeName = "mood")]
            public Mood Mood { get; set; }
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public class MappedSizedDataTypes
        {
            // ReSharper disable once UnusedAutoPropertyAccessor.Global
            public int Id { get; set; }
            /*
            public string Char { get; set; }
            public string Character { get; set; }
            public string Varchar { get; set; }
            public string Char_varying { get; set; }
            public string Character_varying { get; set; }
            public string Nchar { get; set; }
            public string National_character { get; set; }
            public string Nvarchar { get; set; }
            public string National_char_varying { get; set; }
            public string National_character_varying { get; set; }
            public byte[] Binary { get; set; }
            public byte[] Varbinary { get; set; }
            public byte[] Binary_varying { get; set; }
            */
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public class MappedScaledDataTypes
        {
            // ReSharper disable once UnusedAutoPropertyAccessor.Global
            public int Id { get; set; }
            /*
            public float Float { get; set; }
            public float Double_precision { get; set; }
            public DateTimeOffset Datetimeoffset { get; set; }
            public DateTime Datetime2 { get; set; }
            public decimal Decimal { get; set; }
            public decimal Dec { get; set; }
            public decimal Numeric { get; set; }
            */
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public class MappedPrecisionAndScaledDataTypes
        {
            // ReSharper disable once UnusedAutoPropertyAccessor.Global
            public int Id { get; set; }
            /*
            public decimal Decimal { get; set; }
            public decimal Dec { get; set; }
            public decimal Numeric { get; set; }
            */
        }

        // ReSharper disable once MemberCanBePrivate.Global
        protected class MappedNullableDataTypes
        {
            [Column(TypeName = "int")]
            public int? Int { get; set; }

            [Column(TypeName = "bigint")]
            public long? LongAsBigint { get; set; }

            [Column(TypeName = "smallint")]
            public short? ShortAsSmallint { get; set; }

            [Column(TypeName = "smallint")]
            public byte? ByteAsSmallint { get; set; }

            [Column(TypeName = "int")]
            public uint? UintAsInt { get; set; }

            [Column(TypeName = "bigint")]
            public uint? UintAsBigint { get; set; }

            [Column(TypeName = "bigint")]
            public ulong? UlongAsBigint { get; set; }

            [Column(TypeName = "smallint")]
            public ushort? UShortAsSmallint { get; set; }

            [Column(TypeName = "int")]
            public ushort? UShortAsInt { get; set; }

            //[Column(TypeName = "tinyint")]
            //public sbyte? SByteAsTinyint { get; set; }

            [Column(TypeName = "boolean")]
            public bool? BoolAsBoolean { get; set; }

            [Column(TypeName = "numeric")]
            public decimal? Decimal { get; set; }  // decimal is just an alias for numeric

            [Column(TypeName = "numeric")]
            public decimal? DecimalAsNumeric { get; set; }

            [Column(TypeName = "money")]
            public decimal? DecimalAsMoney { get; set; }

            [Column(TypeName = "double precision")]
            public double? DoubleAsDoublePrecision { get; set; }

            [Column(TypeName = "real")]
            public float? FloatAsReal { get; set; }

            [Column(TypeName = "timestamp")]
            public DateTime? DateTimeAsTimestamp { get; set; }

            [Column(TypeName = "timestamptz")]
            public DateTime? DateTimeAsTimestamptz { get; set; }

            [Column(TypeName = "date")]
            public DateTime? DateTimeAsDate { get; set; }

            [Column(TypeName = "time")]
            public TimeSpan? TimeSpanAsTime { get; set; }

            [Column(TypeName = "timetz")]
            public DateTimeOffset? DateTimeOffsetAsTimetz { get; set; }

            [Column(TypeName = "interval")]
            public TimeSpan? TimeSpanAsInterval { get; set; }

            [Column(TypeName = "text")]
            public string StringAsText { get; set; }

            [Column(TypeName = "varchar")]
            public string StringAsVarchar { get; set; }

            // TODO: enable here (and above) after https://github.com/aspnet/EntityFrameworkCore/issues/14159 is fixed
            // [Column(TypeName = "char(1)")]
            // public char? CharAsChar1 { get; set; }

            [Column(TypeName = "text")]
            public char? CharAsText { get; set; }

            [Column(TypeName = "varchar")]
            public char? CharAsVarchar { get; set; }

            [Column(TypeName = "bytea")]
            public byte[] BytesAsBytea { get; set; }

            [Column(TypeName = "uuid")]
            public Guid? GuidAsUuid { get; set; }

            [Column(TypeName = "text")]
            public StringEnum16? EnumAsText { get; set; }

            [Column(TypeName = "varchar")]
            public StringEnumU16? EnumAsVarchar { get; set; }

            // PostgreSQL-specific types from here

            [Column(TypeName = "macaddr")]
            public PhysicalAddress PhysicalAddressAsMacaddr { get; set; }

            [Column(TypeName = "point")]
            public NpgsqlPoint? NpgsqlPointAsPoint { get; set; }

            [Column(TypeName = "jsonb")]
            public string StringAsJsonb { get; set; }

            [Column(TypeName = "json")]
            public string StringAsJson { get; set; }

            [Column(TypeName = "hstore")]
            public Dictionary<string, string> DictionaryAsHstore { get; set; }

            [Column(TypeName = "int4range")]
            public NpgsqlRange<int>? NpgsqlRangeAsRange { get; set; }

            [Column(TypeName = "int[]")]
            public int[] IntArrayAsIntArray { get; set; }

            [Column(TypeName = "macaddr[]")]
            public PhysicalAddress[] PhysicalAddressArrayAsMacaddrArray { get; set; }

            [Column(TypeName = "xid")]
            public uint? UintAsXid { get; set; }

            public NpgsqlTsQuery SearchQuery { get; set; }
            public NpgsqlTsVector SearchVector { get; set; }
            public NpgsqlTsRankingNormalization? RankingNormalization { get; set; }
            public uint? Regconfig { get; set; }

            [Column(TypeName = "mood")]
            public Mood? Mood { get; set; }
        }
    }

    // ReSharper disable once UnusedMember.Global
    public enum Mood { Happy, Sad };
}
