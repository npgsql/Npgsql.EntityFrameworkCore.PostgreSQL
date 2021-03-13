using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.TestUtilities;
using NpgsqlTypes;
using Xunit;
using Xunit.Abstractions;

namespace Npgsql.EntityFrameworkCore.PostgreSQL
{
    public class NodaTimeTest : IClassFixture<NodaTimeTest.NodaTimeFixture>
    {
        public NodaTimeTest(NodaTimeFixture fixture, ITestOutputHelper testOutputHelper)
        {
            Fixture = fixture;
            Fixture.TestSqlLoggerFactory.Clear();
            //Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
        }

        NodaTimeFixture Fixture { get; }

        [Fact]
        public void Operator()
        {
            using var ctx = CreateContext();
            var d = ctx.NodaTimeTypes.Single(t => t.LocalDate < new LocalDate(2018, 4, 21));

            Assert.Equal(new LocalDate(2018, 4, 20), d.LocalDate);
            Assert.Contains(@"n.""LocalDate"" < DATE '2018-04-21'", Sql);
        }

        #region Addition and subtraction

        [Fact]
        public void Add_LocalDate_Period()
        {
            // Note: requires some special type inference logic because we're adding things of different types
            using var ctx = CreateContext();
            var d = ctx.NodaTimeTypes.Single(t => t.LocalDate + Period.FromMonths(1) > t.LocalDate);

            Assert.Equal(new LocalDate(2018, 4, 20), d.LocalDate);
            Assert.Contains(@"(n.""LocalDate"" + INTERVAL 'P1M') > n.""LocalDate""", Sql);
        }

        [Fact]
        public void Subtract_Instant()
        {
            using var ctx = CreateContext();
            var id = ctx.NodaTimeTypes
                .Where(t => t.Instant + Duration.FromDays(1) - t.Instant == Duration.FromDays(1))
                .Select(t => t.Id)
                .Single();

            Assert.Equal(1, id);
            AssertSql(
                @"SELECT n.""Id""
FROM ""NodaTimeTypes"" AS n
WHERE ((n.""Instant"" + INTERVAL '1 00:00:00') - n.""Instant"") = INTERVAL '1 00:00:00'
LIMIT 2");
        }

        [Fact]
        public void Subtract_LocalDateTime()
        {
            using var ctx = CreateContext();
            var id = ctx.NodaTimeTypes
                .Where(t => t.LocalDateTime + Period.FromDays(1) - t.LocalDateTime == Period.FromDays(1))
                .Select(t => t.Id)
                .Single();

            Assert.Equal(1, id);
            AssertSql(
                @"SELECT n.""Id""
FROM ""NodaTimeTypes"" AS n
WHERE ((n.""LocalDateTime"" + INTERVAL 'P1D') - n.""LocalDateTime"") = INTERVAL 'P1D'
LIMIT 2");
        }

        [Fact]
        public void Subtract_ZonedDateTime()
        {
            using var ctx = CreateContext();
            var id = ctx.NodaTimeTypes
                .Where(t => t.ZonedDateTime + Duration.FromDays(1) - t.ZonedDateTime == Duration.FromDays(1))
                .Select(t => t.Id)
                .Single();

            Assert.Equal(1, id);
            AssertSql(
                @"SELECT n.""Id""
FROM ""NodaTimeTypes"" AS n
WHERE ((n.""ZonedDateTime"" + INTERVAL '1 00:00:00') - n.""ZonedDateTime"") = INTERVAL '1 00:00:00'
LIMIT 2");
        }

        [Fact]
        public void Subtract_LocalDate()
        {
            using var ctx = CreateContext();
            var id = ctx.NodaTimeTypes
                .Where(t => t.LocalDate2 - t.LocalDate == Period.FromDays(1))
                .Select(t => t.Id)
                .Single();

            Assert.Equal(1, id);
            AssertSql(
                @"SELECT n.""Id""
FROM ""NodaTimeTypes"" AS n
WHERE MAKE_INTERVAL(days => n.""LocalDate2"" - n.""LocalDate"") = INTERVAL 'P1D'
LIMIT 2");
        }

        [Fact]
        public void Subtract_LocalTime()
        {
            using var ctx = CreateContext();
            var id = ctx.NodaTimeTypes
                .Where(t => t.LocalTime + Period.FromHours(1) - t.LocalTime == Period.FromHours(1))
                .Select(t => t.Id)
                .Single();

            Assert.Equal(1, id);
            AssertSql(
                @"SELECT n.""Id""
FROM ""NodaTimeTypes"" AS n
WHERE ((n.""LocalTime"" + INTERVAL 'PT1H') - n.""LocalTime"") = INTERVAL 'PT1H'
LIMIT 2");
        }

        #endregion

        #region LocalDateTime members

        [Fact]
        public void Select_LocalDateTime_year_component()
        {
            using var ctx = CreateContext();
            var d = ctx.NodaTimeTypes.Single(t => t.LocalDateTime.Year == 2018);

            Assert.Equal(new LocalDateTime(2018, 4, 20, 10, 31, 33, 666), d.LocalDateTime);
            Assert.Contains(@"DATE_PART('year', n.""LocalDateTime"")::INT = 2018", Sql);
        }

        [Fact]
        public void Select_LocalDateTime_month_component()
        {
            using var ctx = CreateContext();
            var d = ctx.NodaTimeTypes.Single(t => t.LocalDateTime.Month == 4);

            Assert.Equal(new LocalDateTime(2018, 4, 20, 10, 31, 33, 666), d.LocalDateTime);
            Assert.Contains(@"DATE_PART('month', n.""LocalDateTime"")::INT = 4", Sql);
        }

        [Fact]
        public void Select_LocalDateTime_day_of_year_component()
        {
            using var ctx = CreateContext();
            var d = ctx.NodaTimeTypes.Single(t => t.LocalDateTime.DayOfYear == 110);

            Assert.Equal(new LocalDateTime(2018, 4, 20, 10, 31, 33, 666), d.LocalDateTime);
            Assert.Contains(@"DATE_PART('doy', n.""LocalDateTime"")::INT = 110", Sql);
        }

        [Fact]
        public void Select_LocalDateTime_day_component()
        {
            using var ctx = CreateContext();
            var d = ctx.NodaTimeTypes.Single(t => t.LocalDateTime.Day == 20);

            Assert.Equal(new LocalDateTime(2018, 4, 20, 10, 31, 33, 666), d.LocalDateTime);
            Assert.Contains(@"DATE_PART('day', n.""LocalDateTime"")::INT = 20", Sql);
        }

        [Fact]
        public void Select_LocalDateTime_hour_component()
        {
            using var ctx = CreateContext();
            var d = ctx.NodaTimeTypes.Single(t => t.LocalDateTime.Hour == 10);

            Assert.Equal(new LocalDateTime(2018, 4, 20, 10, 31, 33, 666), d.LocalDateTime);
            Assert.Contains(@"DATE_PART('hour', n.""LocalDateTime"")::INT = 10", Sql);
        }

        [Fact]
        public void Select_LocalDateTime_minute_component()
        {
            using var ctx = CreateContext();
            var d = ctx.NodaTimeTypes.Single(t => t.LocalDateTime.Minute == 31);

            Assert.Equal(new LocalDateTime(2018, 4, 20, 10, 31, 33, 666), d.LocalDateTime);
            Assert.Contains(@"DATE_PART('minute', n.""LocalDateTime"")::INT = 31", Sql);
        }

        [Fact]
        public void Select_LocalDateTime_second_component()
        {
            using var ctx = CreateContext();
            var d = ctx.NodaTimeTypes.Single(t => t.LocalDateTime.Second == 33);

            Assert.Equal(new LocalDateTime(2018, 4, 20, 10, 31, 33, 666), d.LocalDateTime);
            Assert.Contains(@"FLOOR(DATE_PART('second', n.""LocalDateTime""))::INT = 33", Sql);
        }

        [Fact]
        public void Select_LocalDateTime_date_component()
        {
            using var ctx = CreateContext();
            var d = ctx.NodaTimeTypes.Single(t => t.LocalDateTime.Date == new LocalDate(2018, 4, 20));

            Assert.Equal(new LocalDateTime(2018, 4, 20, 10, 31, 33, 666), d.LocalDateTime);
            Assert.Contains(@"DATE_TRUNC('day', n.""LocalDateTime"") = DATE '2018-04-20'", Sql);
        }

        [Fact]
        public void Select_LocalDateTime_day_of_week_component()
        {
            using var ctx = CreateContext();
            var d = ctx.NodaTimeTypes
                .Where(t => t.LocalDateTime.DayOfWeek == IsoDayOfWeek.Friday)
                .Select(t => t.LocalDateTime)
                .Single();

            Assert.Equal(new LocalDateTime(2018, 4, 20, 10, 31, 33, 666), d);
            AssertSql(@"SELECT n.""LocalDateTime""
FROM ""NodaTimeTypes"" AS n
WHERE CASE
    WHEN FLOOR(DATE_PART('dow', n.""LocalDateTime""))::INT = 0 THEN 7
    ELSE FLOOR(DATE_PART('dow', n.""LocalDateTime""))::INT
END = 5
LIMIT 2");
        }

        #endregion LocalDateTime members

        #region LocalDate members

        [Fact]
        public void Select_LocalDate_year_component()
        {
            using var ctx = CreateContext();
            var d = ctx.NodaTimeTypes.Single(t => t.LocalDate.Year == 2018);

            Assert.Equal(new LocalDate(2018, 4, 20), d.LocalDate);
            Assert.Contains(@"DATE_PART('year', n.""LocalDate"")::INT = 2018", Sql);
        }

        [Fact]
        public void Select_LocalDate_month_component()
        {
            using var ctx = CreateContext();
            var d = ctx.NodaTimeTypes.Single(t => t.LocalDate.Month == 4);

            Assert.Equal(new LocalDate(2018, 4, 20), d.LocalDate);
            Assert.Contains(@"DATE_PART('month', n.""LocalDate"")::INT = 4", Sql);
        }

        [Fact]
        public void Select_LocalDate_day_of_year_component()
        {
            using var ctx = CreateContext();
            var d = ctx.NodaTimeTypes.Single(t => t.LocalDate.DayOfYear == 110);

            Assert.Equal(new LocalDate(2018, 4, 20), d.LocalDate);
            Assert.Contains(@"DATE_PART('doy', n.""LocalDate"")::INT = 110", Sql);
        }

        [Fact]
        public void Select_LocalDate_day_component()
        {
            using var ctx = CreateContext();
            var d = ctx.NodaTimeTypes.Single(t => t.LocalDate.Day == 20);

            Assert.Equal(new LocalDate(2018, 4, 20), d.LocalDate);
            Assert.Contains(@"DATE_PART('day', n.""LocalDate"")::INT = 20", Sql);
        }

        #endregion LocalDate members

        #region LocalTime members

        [Fact]
        public void Select_LocalTime_hour_component()
        {
            using var ctx = CreateContext();
            var d = ctx.NodaTimeTypes.Single(t => t.LocalTime.Hour == 10);

            Assert.Equal(new LocalTime(10, 31, 33, 666), d.LocalTime);
            Assert.Contains(@"DATE_PART('hour', n.""LocalTime"")::INT = 10", Sql);
        }

        [Fact]
        public void Select_LocalTime_minute_component()
        {
            using var ctx = CreateContext();
            var d = ctx.NodaTimeTypes.Single(t => t.LocalTime.Minute == 31);

            Assert.Equal(new LocalTime(10, 31, 33, 666), d.LocalTime);
            Assert.Contains(@"DATE_PART('minute', n.""LocalTime"")::INT = 31", Sql);
        }

        [Fact]
        public void Select_LocalTime_second_component()
        {
            using var ctx = CreateContext();
            var d = ctx.NodaTimeTypes.Single(t => t.LocalTime.Second == 33);

            Assert.Equal(new LocalTime(10, 31, 33, 666), d.LocalTime);
            Assert.Contains(@"FLOOR(DATE_PART('second', n.""LocalTime""))::INT = 33", Sql);
        }

        #endregion LocalTime members

        #region Period

        [Fact]
        public void Select_Period_year_component()
        {
            using var ctx = CreateContext();
            var d = ctx.NodaTimeTypes.Single(t => t.Period.Years == 2018);

            Assert.Equal(_defaultPeriod, d.Period);
            Assert.Contains(@"DATE_PART('year', n.""Period"")::INT = 2018", Sql);
        }

        /*
        [Fact]
        public void Select_Period_month_component()
        {
            using var ctx = CreateContext();
            var d = ctx.NodaTimeTypes.Single(t => t.Period.Month == 4);

            Assert.Equal(new Period(2018, 4, 20, 10, 31, 33, 666), d.Period);
            Assert.Contains(@"WHERE CAST(DATE_PART('month', t.""Period"") AS integer) = 4", Sql);
        }

        [Fact]
        public void Select_Period_day_of_year_component()
        {
            using var ctx = CreateContext();
            var d = ctx.NodaTimeTypes.Single(t => t.Period.DayOfYear == 110);

            Assert.Equal(new Period(2018, 4, 20, 10, 31, 33, 666), d.Period);
            Assert.Contains(@"WHERE CAST(DATE_PART('doy', t.""Period"") AS integer) = 110", Sql);
        }

        [Fact]
        public void Select_Period_day_component()
        {
            using var ctx = CreateContext();
            var d = ctx.NodaTimeTypes.Single(t => t.Period.Day == 20);

            Assert.Equal(new Period(2018, 4, 20, 10, 31, 33, 666), d.Period);
            Assert.Contains(@"WHERE CAST(DATE_PART('day', t.""Period"") AS integer) = 20", Sql);
        }

        [Fact]
        public void Select_Period_hour_component()
        {
            using var ctx = CreateContext();
            var d = ctx.NodaTimeTypes.Single(t => t.Period.Hour == 10);

            Assert.Equal(new Period(2018, 4, 20, 10, 31, 33, 666), d.Period);
            Assert.Contains(@"WHERE CAST(DATE_PART('hour', t.""Period"") AS integer) = 10", Sql);
        }

        [Fact]
        public void Select_Period_minute_component()
        {
            using var ctx = CreateContext();
            var d = ctx.NodaTimeTypes.Single(t => t.Period.Minute == 31);

            Assert.Equal(new Period(2018, 4, 20, 10, 31, 33, 666), d.Period);
            Assert.Contains(@"WHERE CAST(DATE_PART('minute', t.""Period"") AS integer) = 31", Sql);
        }

        [Fact]
        public void Select_Period_second_component()
        {
            using var ctx = CreateContext();
            var d = ctx.NodaTimeTypes.Single(t => t.Period.Second == 33);

            Assert.Equal(new Period(2018, 4, 20, 10, 31, 33, 666), d.Period);
            Assert.Contains(@"WHERE CAST(FLOOR(DATE_PART('second', t.""Period"")) AS integer) = 33", Sql);
        }

        [Fact]
        public void Select_Period_date_component()
        {
            using var ctx = CreateContext();
            var d = ctx.NodaTimeTypes.Single(t => t.Period.Date == new LocalDate(2018, 4, 20));

            Assert.Equal(new Period(2018, 4, 20, 10, 31, 33, 666), d.Period);
            Assert.Contains(@"WHERE DATE_TRUNC('day', t.""Period"") = DATE '2018-04-20'", Sql);
        }

        [Fact]
        public void Select_Period_day_of_week_component()
        {
            using var ctx = CreateContext();
            var d = ctx.NodaTimeTypes.Single(t => t.Period.DayOfWeek == IsoDayOfWeek.Friday);

            Assert.Equal(new Period(2018, 4, 20, 10, 31, 33, 666), d.Period);
            Assert.Contains(@"DATE_PART('dow', t.""Period"")", Sql);
        }
*/

        [Fact]
        public void Period_from_days()
        {
            using var ctx = CreateContext();

            // ReSharper disable once PossibleUnintendedReferenceComparison
            Assert.Null(ctx.NodaTimeTypes.SingleOrDefault(t => t.Period == Period.FromDays(t.Id)));
            Assert.Contains(@"MAKE_INTERVAL(days => n.""Id"")", Sql);
        }

        [Fact]
        public void Period_from_seconds()
        {
            using var ctx = CreateContext();

            // ReSharper disable once PossibleUnintendedReferenceComparison
            Assert.Null(ctx.NodaTimeTypes.SingleOrDefault(t => t.Period == Period.FromSeconds(t.Id)));
            Assert.Contains(@"MAKE_INTERVAL(secs => CAST(n.""Id"" AS bigint))", Sql);
        }

        #endregion Period

        #region Range

        [Fact]
        public void DateRange_Contains()
        {
            using var ctx = CreateContext();
            var _ = ctx.NodaTimeTypes.Single(t => t.DateRange.Contains(new LocalDate(2018, 4, 21)));

            Assert.Contains(@"n.""DateRange"" @> DATE '2018-04-21'", Sql);
        }

        [Fact]
        public void DateInterval_Contains()
        {
            using var ctx = CreateContext();

            var interval = new DateInterval(
                new LocalDate(2018, 01, 01),
                new LocalDate(2020, 12, 25));
            var _ = ctx.NodaTimeTypes.Single(t => interval.Contains(t.LocalDate));

            Assert.Contains(@"@__interval_0 @> n.""LocalDate""", Sql);
        }

        #endregion Range

        #region GetCurrentInstant()

        [Fact]
        public void GetCurrentInstant_from_Instance()
        {
            using var ctx = CreateContext();
            var _ = ctx.NodaTimeTypes.Where(t => t.Instant < SystemClock.Instance.GetCurrentInstant()).ToArray();

            Assert.Contains(@"WHERE n.""Instant"" < NOW() AT TIME ZONE 'UTC'", Sql);
        }

        #endregion

        #region Support

        NodaTimeContext CreateContext() => Fixture.CreateContext();

        string Sql => Fixture.TestSqlLoggerFactory.Sql;

        static Period _defaultPeriod;

        public class NodaTimeFixture : SharedStoreFixtureBase<NodaTimeContext>
        {
            protected override string StoreName { get; } = "NodaTimeTest";

            protected override IServiceCollection AddServices(IServiceCollection serviceCollection)
                => base.AddServices(serviceCollection).AddEntityFrameworkNpgsqlNodaTime();

            public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
            {
                var optionsBuilder = base.AddOptions(builder);
                new NpgsqlDbContextOptionsBuilder(optionsBuilder).UseNodaTime();

                return optionsBuilder;
            }

            protected override void Seed(NodaTimeContext context)
                => NodaTimeContext.Seed(context);

            protected override ITestStoreFactory TestStoreFactory => NpgsqlTestStoreFactory.Instance;

            public TestSqlLoggerFactory TestSqlLoggerFactory => (TestSqlLoggerFactory)ServiceProvider.GetRequiredService<ILoggerFactory>();
        }

        public class NodaTimeContext : PoolableDbContext
        {
            public NodaTimeContext(DbContextOptions<NodaTimeContext> options) : base(options) {}

            // ReSharper disable once MemberHidesStaticFromOuterClass
            // ReSharper disable once UnusedAutoPropertyAccessor.Global
            public DbSet<NodaTimeTypes> NodaTimeTypes { get; set; }

            public static void Seed(NodaTimeContext context)
            {
                var localDateTime = new LocalDateTime(2018, 4, 20, 10, 31, 33, 666);
                var zonedDateTime = localDateTime.InUtc();
                var instant = zonedDateTime.ToInstant();

                _defaultPeriod = Period.FromYears(2018) + Period.FromMonths(4) + Period.FromDays(20) +
                                Period.FromHours(10) + Period.FromMinutes(31) + Period.FromSeconds(23) +
                                Period.FromMilliseconds(666);
                context.Add(new NodaTimeTypes
                {
                    Id = 1,
                    LocalDateTime = localDateTime,
                    ZonedDateTime = zonedDateTime,
                    Instant = instant,
                    LocalDate = localDateTime.Date,
                    LocalDate2 = localDateTime.Date + Period.FromDays(1),
                    LocalTime = localDateTime.TimeOfDay,
                    OffsetTime = new OffsetTime(new LocalTime(10, 31, 33, 666), Offset.Zero),
                    Period = _defaultPeriod,
                    DateRange = new NpgsqlRange<LocalDate>(localDateTime.Date, localDateTime.Date.PlusDays(5))
                });
                context.SaveChanges();
            }
        }

        public class NodaTimeTypes
        {
            // ReSharper disable UnusedAutoPropertyAccessor.Global
            public int Id { get; set; }
            public Instant Instant { get; set; }
            public LocalDateTime LocalDateTime { get; set; }
            public ZonedDateTime ZonedDateTime { get; set; }
            public LocalDate LocalDate { get; set; }
            public LocalDate LocalDate2 { get; set; }
            public LocalTime LocalTime { get; set; }
            public OffsetTime OffsetTime { get; set; }
            public Period Period { get; set; }
            public NpgsqlRange<LocalDate> DateRange { get; set; }
            // ReSharper restore UnusedAutoPropertyAccessor.Global
        }

        void AssertSql(params string[] expected)
            => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);

        #endregion Support
    }
}
