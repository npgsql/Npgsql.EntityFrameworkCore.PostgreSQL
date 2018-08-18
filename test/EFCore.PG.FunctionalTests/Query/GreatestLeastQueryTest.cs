﻿using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql.EntityFrameworkCore.PostgreSQL.TestUtilities;
using Xunit;

namespace Npgsql.EntityFrameworkCore.PostgreSQL.Query
{
    /// <summary>
    /// Provides unit tests for GREATEST(...) and LEAST(...).
    /// </summary>
    /// <remarks>
    /// See: https://www.postgresql.org/docs/current/static/functions-conditional.html#FUNCTIONS-GREATEST-LEAST
    /// </remarks>
    public class GreatestLeastQueryTest : IClassFixture<GreatestLeastQueryTest.GreatestLeastQueryNpgsqlFixture>
    {
        #region Tests

        #region Greatest

        [Fact]
        public void Max_ushort_ushort()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var _ =
                    ctx.GreatestLeastTestEntities
                       .Select(x => Math.Max(x.UShort, x.UShort))
                       .ToArray();

                AssertContainsSql("SELECT GREATEST(x.\"UShort\", x.\"UShort\")");
            }
        }

        [Fact]
        public void Max_uint_uint()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var _ =
                    ctx.GreatestLeastTestEntities
                       .Select(x => Math.Max(x.UInt, x.UInt))
                       .ToArray();

                AssertContainsSql("SELECT GREATEST(x.\"UInt\", x.\"UInt\")");
            }
        }

        [Fact]
        public void Max_ulong_ulong()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var _ =
                    ctx.GreatestLeastTestEntities
                       .Select(x => Math.Max(x.ULong, x.ULong))
                       .ToArray();

                AssertContainsSql("SELECT GREATEST(x.\"ULong\", x.\"ULong\")");
            }
        }

        [Fact]
        public void Max_short_short()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var _ =
                    ctx.GreatestLeastTestEntities
                       .Select(x => Math.Max(x.Short, x.Short))
                       .ToArray();

                AssertContainsSql("SELECT GREATEST(x.\"Short\", x.\"Short\")");
            }
        }

        [Fact]
        public void Max_int_int()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var _ =
                    ctx.GreatestLeastTestEntities
                       .Select(x => Math.Max(x.Int, x.Int))
                       .ToArray();

                AssertContainsSql("SELECT GREATEST(x.\"Int\", x.\"Int\")");
            }
        }

        [Fact]
        public void Max_long_long()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var _ =
                    ctx.GreatestLeastTestEntities
                       .Select(x => Math.Max(x.Long, x.Long))
                       .ToArray();

                AssertContainsSql("SELECT GREATEST(x.\"Long\", x.\"Long\")");
            }
        }

        [Fact]
        public void Max_float_float()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var _ =
                    ctx.GreatestLeastTestEntities
                       .Select(x => Math.Max(x.Float, x.Float))
                       .ToArray();

                AssertContainsSql("SELECT GREATEST(x.\"Float\", x.\"Float\")");
            }
        }

        [Fact]
        public void Max_double_double()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var _ =
                    ctx.GreatestLeastTestEntities
                       .Select(x => Math.Max(x.Double, x.Double))
                       .ToArray();

                AssertContainsSql("SELECT GREATEST(x.\"Double\", x.\"Double\")");
            }
        }

        [Fact]
        public void Max_decimal_decimal()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var _ =
                    ctx.GreatestLeastTestEntities
                       .Select(x => Math.Max(x.Decimal, x.Decimal))
                       .ToArray();

                AssertContainsSql("SELECT GREATEST(x.\"Decimal\", x.\"Decimal\")");
            }
        }

        [Fact]
        public void Max_sbyte_sbyte()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var _ =
                    ctx.GreatestLeastTestEntities
                       .Select(x => Math.Max(x.SByte, x.SByte))
                       .ToArray();

                AssertContainsSql("SELECT GREATEST(x.\"SByte\", x.\"SByte\")");
            }
        }

        [Fact]
        public void Max_byte_byte()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var _ =
                    ctx.GreatestLeastTestEntities
                       .Select(x => Math.Max(x.Byte, x.Byte))
                       .ToArray();

                AssertContainsSql("SELECT GREATEST(x.\"Byte\", x.\"Byte\")");
            }
        }

        #endregion

        #region Least

        [Fact]
        public void Min_ushort_ushort()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var _ =
                    ctx.GreatestLeastTestEntities
                       .Select(x => Math.Min(x.UShort, x.UShort))
                       .ToArray();

                AssertContainsSql("SELECT LEAST(x.\"UShort\", x.\"UShort\")");
            }
        }

        [Fact]
        public void Min_uint_uint()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var _ =
                    ctx.GreatestLeastTestEntities
                       .Select(x => Math.Min(x.UInt, x.UInt))
                       .ToArray();

                AssertContainsSql("SELECT LEAST(x.\"UInt\", x.\"UInt\")");
            }
        }

        [Fact]
        public void Min_ulong_ulong()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var _ =
                    ctx.GreatestLeastTestEntities
                       .Select(x => Math.Min(x.ULong, x.ULong))
                       .ToArray();

                AssertContainsSql("SELECT LEAST(x.\"ULong\", x.\"ULong\")");
            }
        }

        [Fact]
        public void Min_short_short()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var _ =
                    ctx.GreatestLeastTestEntities
                       .Select(x => Math.Min(x.Short, x.Short))
                       .ToArray();

                AssertContainsSql("SELECT LEAST(x.\"Short\", x.\"Short\")");
            }
        }

        [Fact]
        public void Min_int_int()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var _ =
                    ctx.GreatestLeastTestEntities
                       .Select(x => Math.Min(x.Int, x.Int))
                       .ToArray();

                AssertContainsSql("SELECT LEAST(x.\"Int\", x.\"Int\")");
            }
        }

        [Fact]
        public void Min_long_long()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var _ =
                    ctx.GreatestLeastTestEntities
                       .Select(x => Math.Min(x.Long, x.Long))
                       .ToArray();

                AssertContainsSql("SELECT LEAST(x.\"Long\", x.\"Long\")");
            }
        }

        [Fact]
        public void Min_float_float()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var _ =
                    ctx.GreatestLeastTestEntities
                       .Select(x => Math.Min(x.Float, x.Float))
                       .ToArray();

                AssertContainsSql("SELECT LEAST(x.\"Float\", x.\"Float\")");
            }
        }

        [Fact]
        public void Min_double_double()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var _ =
                    ctx.GreatestLeastTestEntities
                       .Select(x => Math.Min(x.Double, x.Double))
                       .ToArray();

                AssertContainsSql("SELECT LEAST(x.\"Double\", x.\"Double\")");
            }
        }

        [Fact]
        public void Min_decimal_decimal()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var _ =
                    ctx.GreatestLeastTestEntities
                       .Select(x => Math.Min(x.Decimal, x.Decimal))
                       .ToArray();

                AssertContainsSql("SELECT LEAST(x.\"Decimal\", x.\"Decimal\")");
            }
        }

        [Fact]
        public void Min_sbyte_sbyte()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var _ =
                    ctx.GreatestLeastTestEntities
                       .Select(x => Math.Min(x.SByte, x.SByte))
                       .ToArray();

                AssertContainsSql("SELECT LEAST(x.\"SByte\", x.\"SByte\")");
            }
        }

        [Fact]
        public void Min_byte_byte()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var _ =
                    ctx.GreatestLeastTestEntities
                       .Select(x => Math.Min(x.Byte, x.Byte))
                       .ToArray();

                AssertContainsSql("SELECT LEAST(x.\"Byte\", x.\"Byte\")");
            }
        }

        #endregion

        #endregion

        #region Setup

        /// <summary>
        /// Provides resources for unit tests.
        /// </summary>
        GreatestLeastQueryNpgsqlFixture Fixture { get; }

        /// <summary>
        /// Initializes resources for unit tests.
        /// </summary>
        /// <param name="fixture"> The fixture of resources for testing. </param>
        public GreatestLeastQueryTest(GreatestLeastQueryNpgsqlFixture fixture)
        {
            Fixture = fixture;
            Fixture.TestSqlLoggerFactory.Clear();
        }

        #endregion

        #region Fixtures

        /// <summary>
        /// Represents a fixture suitable for testing GREATEST(...) and LEAST(...)/
        /// </summary>
        public class GreatestLeastQueryNpgsqlFixture : IDisposable
        {
            /// <summary>
            /// The <see cref="NpgsqlTestStore"/> used for testing.
            /// </summary>
            private readonly NpgsqlTestStore _testStore;

            /// <summary>
            /// The <see cref="DbContextOptions"/> used for testing.
            /// </summary>
            private readonly DbContextOptions _options;

            /// <summary>
            /// The logger factory used for testing.
            /// </summary>
            public TestSqlLoggerFactory TestSqlLoggerFactory { get; }

            /// <summary>
            /// Initializes a <see cref="GreatestLeastQueryNpgsqlFixture"/>.
            /// </summary>
            // ReSharper disable once UnusedMember.Global
            public GreatestLeastQueryNpgsqlFixture()
            {
                TestSqlLoggerFactory = new TestSqlLoggerFactory();

                _testStore = NpgsqlTestStore.CreateScratch();

                _options =
                    new DbContextOptionsBuilder()
                        .UseNpgsql(_testStore.ConnectionString, b => b.ApplyConfiguration())
                        .UseInternalServiceProvider(
                            new ServiceCollection()
                                .AddEntityFrameworkNpgsql()
                                .AddSingleton<ILoggerFactory>(TestSqlLoggerFactory)
                                .BuildServiceProvider())
                        .Options;

                using (GreatestLeastContext context = CreateContext())
                {
                    context.Database.EnsureCreated();
                }
            }

            /// <summary>
            /// Creates a new <see cref="GreatestLeastContext"/>.
            /// </summary>
            /// <returns>
            /// A <see cref="GreatestLeastContext"/> for testing.
            /// </returns>
            public GreatestLeastContext CreateContext() => new GreatestLeastContext(_options);

            /// <inheritdoc />
            public void Dispose() => _testStore.Dispose();
        }

        /// <summary>
        /// Represents an entity suitable for testing GREATEST(...) and LEAST(...) operators.
        /// </summary>
        public class GreatestLeastTestEntity
        {
            public int Id { get; set; }
            public ushort UShort { get; set; }
            public uint UInt { get; set; }
            public ulong ULong { get; set; }
            public short Short { get; set; }
            public int Int { get; set; }
            public long Long { get; set; }
            public float Float { get; set; }
            public double Double { get; set; }
            public decimal Decimal { get; set; }
            public byte Byte { get; set; }
            public sbyte SByte { get; set; }
        }

        /// <summary>
        /// Represents a database suitable for testing GREATEST(...) and LEAST(...).
        /// </summary>
        public class GreatestLeastContext : DbContext
        {
            /// <summary>
            /// Represents a set of entities with numeric properties.
            /// </summary>
            public DbSet<GreatestLeastTestEntity> GreatestLeastTestEntities { get; set; }

            /// <inheritdoc />
            public GreatestLeastContext(DbContextOptions options) : base(options) {}
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Asserts that the SQL fragment appears in the logs.
        /// </summary>
        /// <param name="sql">The SQL statement or fragment to search for in the logs.</param>
        public void AssertContainsSql(string sql) => Assert.Contains(sql, Fixture.TestSqlLoggerFactory.Sql);

        #endregion
    }
}
