using System;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime;
using NodaTime.Calendars;
using NodaTime.TimeZones;
using Npgsql.EntityFrameworkCore.PostgreSQL.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal;
using Xunit;

namespace Npgsql.EntityFrameworkCore.PostgreSQL
{
    public class NpgsqlNodaTimeTypeMappingTest
    {
        [Fact]
        public void GenerateSqlLiteral_returns_instant_literal()
        {
            var mapping = GetMapping(typeof(Instant));
            Assert.Equal("timestamp", mapping.StoreType);

            var instant = (new LocalDateTime(2018, 4, 20, 10, 31, 33, 666) + Period.FromTicks(6660)).InUtc().ToInstant();
            Assert.Equal("TIMESTAMP '2018-04-20T10:31:33.666666Z'", mapping.GenerateSqlLiteral(instant));
        }

        [Fact]
        public void GenerateSqlLiteral_returns_local_date_time_literal()
        {
            var mapping = GetMapping(typeof(LocalDateTime));
            Assert.Equal("timestamp", mapping.StoreType);

            var localDateTime = new LocalDateTime(2018, 4, 20, 10, 31, 33, 666) + Period.FromTicks(6660);
            Assert.Equal("TIMESTAMP '2018-04-20T10:31:33.666666'", mapping.GenerateSqlLiteral(localDateTime));
        }

        [Fact]
        public void GenerateCodeLiteral_returns_local_date_time_literal()
        {
            Assert.Equal("new NodaTime.LocalDateTime(2018, 4, 20, 10, 31)", CodeLiteral(new LocalDateTime(2018, 4, 20, 10, 31)));
            Assert.Equal("new NodaTime.LocalDateTime(2018, 4, 20, 10, 31, 33)", CodeLiteral(new LocalDateTime(2018, 4, 20, 10, 31, 33)));

            var localDateTime = new LocalDateTime(2018, 4, 20, 10, 31, 33) + Period.FromNanoseconds(1);
            Assert.Equal("new NodaTime.LocalDateTime(2018, 4, 20, 10, 31, 33).PlusNanoseconds(1L)", CodeLiteral(localDateTime));
        }

        [Fact]
        public void GenerateSqlLiteral_returns_timestamptz_instant_literal()
        {
            var mapping = GetMapping(typeof(Instant), "timestamp with time zone");
            Assert.Equal(typeof(Instant), mapping.ClrType);
            Assert.Equal("timestamp with time zone", mapping.StoreType);

            var instant = (new LocalDateTime(2018, 4, 20, 10, 31, 33, 666) + Period.FromTicks(6660)).InUtc().ToInstant();
            Assert.Equal("TIMESTAMPTZ '2018-04-20T10:31:33.666666Z'", mapping.GenerateSqlLiteral(instant));
        }

        [Fact]
        public void GenerateSqlLiteral_returns_zoned_date_time_literal()
        {
            var mapping = GetMapping(typeof(ZonedDateTime));
            Assert.Equal("timestamp with time zone", mapping.StoreType);

            var zonedDateTime = (new LocalDateTime(2018, 4, 20, 10, 31, 33, 666) + Period.FromTicks(6660))
                .InZone(DateTimeZone.ForOffset(Offset.FromHours(2)), Resolvers.LenientResolver);
            Assert.Equal("TIMESTAMPTZ '2018-04-20T10:31:33.666666+02'", mapping.GenerateSqlLiteral(zonedDateTime));
        }

        [Fact]
        public void GenerateCodeLiteral_returns_zoned_date_time_literal()
        {
            var zonedDateTime = (new LocalDateTime(2018, 4, 20, 10, 31, 33, 666) + Period.FromTicks(6660))
                .InZone(DateTimeZone.ForOffset(Offset.FromHours(2)), Resolvers.LenientResolver);
            Assert.Equal(@"new NodaTime.ZonedDateTime(NodaTime.Instant.FromUnixTimeTicks(15242130936666660L), NodaTime.TimeZones.TzdbDateTimeZoneSource.Default.ForId(""UTC+02""))",
                CodeLiteral(zonedDateTime));
        }

        [Fact]
        public void GenerateSqlLiteral_returns_offset_date_time_literal()
        {
            var mapping = GetMapping(typeof(OffsetDateTime));
            Assert.Equal("timestamp with time zone", mapping.StoreType);

            var offsetDateTime = new OffsetDateTime(
                new LocalDateTime(2018, 4, 20, 10, 31, 33, 666) + Period.FromTicks(6660),
                Offset.FromHours(2));
            Assert.Equal("TIMESTAMPTZ '2018-04-20T10:31:33.666666+02'", mapping.GenerateSqlLiteral(offsetDateTime));
        }

        [Fact]
        public void GenerateCodeLiteral_returns_instant_literal()
            => Assert.Equal("NodaTime.Instant.FromUnixTimeTicks(15832607590000000L)",
                CodeLiteral(Instant.FromUtc(2020, 3, 3, 18, 39, 19)));

        [Fact]
        public void GenerateCodeLiteral_returns_offset_date_time_literal()
        {
            Assert.Equal("new NodaTime.OffsetDateTime(new NodaTime.LocalDateTime(2018, 4, 20, 10, 31), NodaTime.Offset.FromHours(-2))",
                CodeLiteral(new OffsetDateTime(new LocalDateTime(2018, 4, 20, 10, 31), Offset.FromHours(-2))));

            Assert.Equal("new NodaTime.OffsetDateTime(new NodaTime.LocalDateTime(2018, 4, 20, 10, 31, 33), NodaTime.Offset.FromSeconds(9000))",
                CodeLiteral(new OffsetDateTime(new LocalDateTime(2018, 4, 20, 10, 31, 33), Offset.FromHoursAndMinutes(2, 30))));

            Assert.Equal("new NodaTime.OffsetDateTime(new NodaTime.LocalDateTime(2018, 4, 20, 10, 31, 33), NodaTime.Offset.FromSeconds(-1))",
                CodeLiteral(new OffsetDateTime(new LocalDateTime(2018, 4, 20, 10, 31, 33), Offset.FromSeconds(-1))));
        }

        [Fact]
        public void GenerateSqlLiteral_returns_local_date_literal()
        {
            var mapping = GetMapping(typeof(LocalDate));

            Assert.Equal("DATE '2018-04-20'", mapping.GenerateSqlLiteral(new LocalDate(2018, 4, 20)));
        }

        [Fact]
        public void GenerateCodeLiteral_returns_local_date_literal()
        {
            Assert.Equal("new NodaTime.LocalDate(2018, 4, 20)", CodeLiteral(new LocalDate(2018, 4, 20)));
            Assert.Equal("new NodaTime.LocalDate(-2017, 4, 20)", CodeLiteral(new LocalDate(Era.BeforeCommon, 2018, 4, 20)));
        }

        [Fact]
        public void GenerateSqlLiteral_returns_local_time_literal()
        {
            var mapping = GetMapping(typeof(LocalTime));

            Assert.Equal("TIME '10:31:33'", mapping.GenerateSqlLiteral(new LocalTime(10, 31, 33)));
            Assert.Equal("TIME '10:31:33.666'", mapping.GenerateSqlLiteral(new LocalTime(10, 31, 33, 666)));
            Assert.Equal("TIME '10:31:33.666666'", mapping.GenerateSqlLiteral(new LocalTime(10, 31, 33, 666) + Period.FromTicks(6660)));
        }

        [Fact]
        public void GenerateCodeLiteral_returns_local_time_literal()
        {
            Assert.Equal("new NodaTime.LocalTime(9, 30)", CodeLiteral(new LocalTime(9, 30)));
            Assert.Equal("new NodaTime.LocalTime(9, 30, 15)", CodeLiteral(new LocalTime(9, 30, 15)));
            Assert.Equal("NodaTime.LocalTime.FromHourMinuteSecondNanosecond(9, 30, 15, 500000000L)", CodeLiteral(new LocalTime(9, 30, 15, 500)));
            Assert.Equal("NodaTime.LocalTime.FromHourMinuteSecondNanosecond(9, 30, 15, 1L)", CodeLiteral(LocalTime.FromHourMinuteSecondNanosecond(9, 30, 15, 1)));
        }

        [Fact]
        public void GenerateSqlLiteral_returns_offset_time_literal()
        {
            var mapping = GetMapping(typeof(OffsetTime));

            Assert.Equal("TIMETZ '10:31:33+02'", mapping.GenerateSqlLiteral(
                new OffsetTime(new LocalTime(10, 31, 33), Offset.FromHours(2))));
            Assert.Equal("TIMETZ '10:31:33-02:30'", mapping.GenerateSqlLiteral(
                new OffsetTime(new LocalTime(10, 31, 33), Offset.FromHoursAndMinutes(-2, -30))));
            Assert.Equal("TIMETZ '10:31:33.666666Z'", mapping.GenerateSqlLiteral(
                new OffsetTime(new LocalTime(10, 31, 33, 666) + Period.FromTicks(6660), Offset.Zero)));
        }

        [Fact]
        public void GenerateCodeLiteral_returns_offset_time_literal()
            => Assert.Equal("new NodaTime.OffsetTime(new NodaTime.LocalTime(10, 31, 33), NodaTime.Offset.FromHours(2))",
                CodeLiteral(new OffsetTime(new LocalTime(10, 31, 33), Offset.FromHours(2))));

        [Fact]
        public void GenerateSqlLiteral_returns_period_literal()
        {
            var mapping = GetMapping(typeof(Period));

            var hms = Period.FromHours(4) + Period.FromMinutes(3) + Period.FromSeconds(2);
            Assert.Equal("INTERVAL 'PT4H3M2S'", mapping.GenerateSqlLiteral(hms));

            var withMilliseconds = hms + Period.FromMilliseconds(1);
            Assert.Equal("INTERVAL 'PT4H3M2.001S'", mapping.GenerateSqlLiteral(withMilliseconds));

            var withMicroseconds = hms + Period.FromTicks(6660);
            Assert.Equal("INTERVAL 'PT4H3M2.000666S'", mapping.GenerateSqlLiteral(withMicroseconds));

            var withYearMonthDay = hms + Period.FromYears(2018) + Period.FromMonths(4) + Period.FromDays(20);
            Assert.Equal("INTERVAL 'P2018Y4M20DT4H3M2S'", mapping.GenerateSqlLiteral(withYearMonthDay));
        }

        [Fact]
        public void GenerateCodeLiteral_returns_period_literal()
        {
            Assert.Equal("NodaTime.Period.FromHours(5L)", CodeLiteral(Period.FromHours(5)));

            Assert.Equal("NodaTime.Period.FromYears(1) + NodaTime.Period.FromMonths(2) + NodaTime.Period.FromWeeks(3) + " +
                         "NodaTime.Period.FromDays(4) + NodaTime.Period.FromHours(5L) + NodaTime.Period.FromMinutes(6L) + " +
                         "NodaTime.Period.FromSeconds(7L) + NodaTime.Period.FromMilliseconds(8L) + NodaTime.Period.FromNanoseconds(9L)",
                CodeLiteral(Period.FromYears(1) + Period.FromMonths(2) + Period.FromWeeks(3) + Period.FromDays(4) + Period.FromHours(5) +
                            Period.FromMinutes(6) + Period.FromSeconds(7) + Period.FromMilliseconds(8) + Period.FromNanoseconds(9)));
        }

        [Fact]
        public void GenerateCodeLiteral_returns_duration_literal()
        {
            Assert.Equal("NodaTime.Duration.FromHours(5)", CodeLiteral(Duration.FromHours(5)));

            Assert.Equal("NodaTime.Duration.FromDays(4) + NodaTime.Duration.FromHours(5) + NodaTime.Duration.FromMinutes(6L) + " +
                         "NodaTime.Duration.FromSeconds(7L) + NodaTime.Duration.FromMilliseconds(8L)",
                CodeLiteral(Duration.FromDays(4) + Duration.FromHours(5) + Duration.FromMinutes(6) + Duration.FromSeconds(7) +
                            Duration.FromMilliseconds(8)));
        }

        [Fact]
        public void GenerateCodeLiteral_returns_dateinterval_literal()
        {
            Assert.Equal("new NodaTime.DateInterval(new NodaTime.LocalDate(2020, 1, 1), new NodaTime.LocalDate(2020, 12, 25))",
                CodeLiteral(new DateInterval(new LocalDate(2020, 01, 01), new LocalDate(2020, 12, 25))));
        }

        [Fact]
        public void GenerateSqlLiteral_returns_dateinterval_literal()
        {
            var mapping = GetMapping(typeof(DateInterval));

            var interval = new DateInterval(
                new LocalDate(2020, 01, 01),
                new LocalDate(2020, 12, 25));
            Assert.Equal("'[2020-01-01,  2020-12-25]'::daterange", mapping.GenerateSqlLiteral(interval));


        }

        #region Support

        static readonly NpgsqlTypeMappingSource Mapper = new(
            new TypeMappingSourceDependencies(
                new ValueConverterSelector(new ValueConverterSelectorDependencies()),
                Array.Empty<ITypeMappingSourcePlugin>()),
            new RelationalTypeMappingSourceDependencies(
                new IRelationalTypeMappingSourcePlugin[] {
                    new NpgsqlNodaTimeTypeMappingSourcePlugin(new NpgsqlSqlGenerationHelper(new RelationalSqlGenerationHelperDependencies()))
                }),
            new NpgsqlSqlGenerationHelper(new RelationalSqlGenerationHelperDependencies()),
            new NpgsqlOptions()
        );

        static RelationalTypeMapping GetMapping(string storeType) => Mapper.FindMapping(storeType);

        static RelationalTypeMapping GetMapping(Type clrType) => (RelationalTypeMapping)Mapper.FindMapping(clrType);

        static RelationalTypeMapping GetMapping(Type clrType, string storeType)
            => Mapper.FindMapping(clrType, storeType);

        static readonly CSharpHelper CsHelper = new(Mapper);

        static string CodeLiteral(object value) => CsHelper.UnknownLiteral(value);

        #endregion Support
    }
}
