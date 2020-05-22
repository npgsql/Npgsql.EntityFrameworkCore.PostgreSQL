using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.EntityFrameworkCore.TestUtilities.Xunit;
using Npgsql.EntityFrameworkCore.PostgreSQL.TestUtilities;
using Xunit;
using Xunit.Abstractions;

namespace Npgsql.EntityFrameworkCore.PostgreSQL.Query
{
    public class ArrayArrayQueryTest : IClassFixture<ArrayArrayQueryTest.ArrayArrayQueryFixture>
    {
        ArrayArrayQueryFixture Fixture { get; }

        public ArrayArrayQueryTest(ArrayArrayQueryFixture fixture, ITestOutputHelper testOutputHelper)
        {
            Fixture = fixture;
            Fixture.TestSqlLoggerFactory.Clear();
            //Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
        }

        #region Roundtrip

        [Fact]
        public void Roundtrip()
        {
            using var ctx = CreateContext();
            var x = ctx.SomeEntities.Single(e => e.Id == 1);

            Assert.Equal(new[] { 3, 4 }, x.SomeArray);
        }

        #endregion

        #region Indexers

        [Fact]
        public void Index_with_constant()
        {
            using var ctx = CreateContext();
            var actual = ctx.SomeEntities.Where(e => e.SomeArray[0] == 3).ToList();

            Assert.Single(actual);
            AssertSql(
                @"SELECT s.""Id"", s.""SomeArray"", s.""SomeByte"", s.""SomeByteArray"", s.""SomeBytea"", s.""SomeMatrix"", s.""SomeStringArray"", s.""SomeText""
FROM ""SomeEntities"" AS s
WHERE s.""SomeArray""[1] = 3");
        }

        [Fact]
        public void Index_with_non_constant()
        {
            using var ctx = CreateContext();
            // ReSharper disable once ConvertToConstant.Local
            var x = 0;
            var actual = ctx.SomeEntities.Where(e => e.SomeArray[x] == 3).ToList();

            Assert.Single(actual);
            AssertSql(
                @"@__x_0='0'

SELECT s.""Id"", s.""SomeArray"", s.""SomeByte"", s.""SomeByteArray"", s.""SomeBytea"", s.""SomeMatrix"", s.""SomeStringArray"", s.""SomeText""
FROM ""SomeEntities"" AS s
WHERE s.""SomeArray""[@__x_0 + 1] = 3");
        }

        #endregion

        #region Equality

        [Fact]
        public void SequenceEqual_with_parameter()
        {
            using var ctx = CreateContext();
            var arr = new[] { 3, 4 };
            var x = ctx.SomeEntities.Single(e => e.SomeArray.SequenceEqual(arr));

            Assert.Equal(new[] { 3, 4 }, x.SomeArray);
            AssertSql(
                @"@__arr_0='System.Int32[]' (DbType = Object)

SELECT s.""Id"", s.""SomeArray"", s.""SomeByte"", s.""SomeByteArray"", s.""SomeBytea"", s.""SomeMatrix"", s.""SomeStringArray"", s.""SomeText""
FROM ""SomeEntities"" AS s
WHERE s.""SomeArray"" = @__arr_0
LIMIT 2");
        }

        [Fact]
        public void SequenceEqual_with_array_literal()
        {
            using var ctx = CreateContext();
            var x = ctx.SomeEntities.Single(e => e.SomeArray.SequenceEqual(new[] { 3, 4 }));

            Assert.Equal(new[] { 3, 4 }, x.SomeArray);
            AssertSql(
                @"SELECT s.""Id"", s.""SomeArray"", s.""SomeByte"", s.""SomeByteArray"", s.""SomeBytea"", s.""SomeMatrix"", s.""SomeStringArray"", s.""SomeText""
FROM ""SomeEntities"" AS s
WHERE s.""SomeArray"" = ARRAY[3,4]::integer[]
LIMIT 2");
        }

        #endregion

        #region Containment

        [Fact]
        public void Contains_with_literal()
        {
            using var ctx = CreateContext();
            var x = ctx.SomeEntities.Single(e => e.SomeArray.Contains(3));

            Assert.Equal(new[] { 3, 4 }, x.SomeArray);
            AssertSql(
                @"SELECT s.""Id"", s.""SomeArray"", s.""SomeByte"", s.""SomeByteArray"", s.""SomeBytea"", s.""SomeMatrix"", s.""SomeStringArray"", s.""SomeText""
FROM ""SomeEntities"" AS s
WHERE 3 = ANY (s.""SomeArray"")
LIMIT 2");
        }

        [Fact]
        public void Contains_with_parameter()
        {
            using var ctx = CreateContext();
            // ReSharper disable once ConvertToConstant.Local
            var p = 3;
            var x = ctx.SomeEntities.Single(e => e.SomeArray.Contains(p));

            Assert.Equal(new[] { 3, 4 }, x.SomeArray);
            AssertSql(
                @"@__p_0='3'

SELECT s.""Id"", s.""SomeArray"", s.""SomeByte"", s.""SomeByteArray"", s.""SomeBytea"", s.""SomeMatrix"", s.""SomeStringArray"", s.""SomeText""
FROM ""SomeEntities"" AS s
WHERE @__p_0 = ANY (s.""SomeArray"")
LIMIT 2");
        }

        [Fact]
        public void Contains_with_column()
        {
            using var ctx = CreateContext();
            var x = ctx.SomeEntities.Single(e => e.SomeArray.Contains(e.Id + 2));

            Assert.Equal(new[] { 3, 4 }, x.SomeArray);
            AssertSql(
                @"SELECT s.""Id"", s.""SomeArray"", s.""SomeByte"", s.""SomeByteArray"", s.""SomeBytea"", s.""SomeMatrix"", s.""SomeStringArray"", s.""SomeText""
FROM ""SomeEntities"" AS s
WHERE s.""Id"" + 2 = ANY (s.""SomeArray"")
LIMIT 2");
        }

        [Fact]
        public void Byte_array_parameter_contains_column()
        {
            using var ctx = CreateContext();
            var values = new byte[] { 20 };
            var x = ctx.SomeEntities.Single(e => values.Contains(e.SomeByte));

            Assert.Equal(2, x.Id);
            // Note: EF Core prints the parameter as a bytea, but it's actually a smallint[] (otherwise ANY would fail)
            AssertSql(
                @"@__values_0='0x14' (DbType = Object)

SELECT s.""Id"", s.""SomeArray"", s.""SomeByte"", s.""SomeByteArray"", s.""SomeBytea"", s.""SomeMatrix"", s.""SomeStringArray"", s.""SomeText""
FROM ""SomeEntities"" AS s
WHERE s.""SomeByte"" = ANY (@__values_0)
LIMIT 2");
        }

        #endregion

        #region Length

        [Fact]
        public void Length()
        {
            using var ctx = CreateContext();
            var x = ctx.SomeEntities.Single(e => e.SomeArray.Length == 2);

            Assert.Equal(new[] { 3, 4 }, x.SomeArray);
            AssertSql(
                @"SELECT s.""Id"", s.""SomeArray"", s.""SomeByte"", s.""SomeByteArray"", s.""SomeBytea"", s.""SomeMatrix"", s.""SomeStringArray"", s.""SomeText""
FROM ""SomeEntities"" AS s
WHERE cardinality(s.""SomeArray"") = 2
LIMIT 2");
        }

        [Fact]
        public void Length_on_EF_Property()
        {
            using var ctx = CreateContext();
            var x = ctx.SomeEntities.Single(e => EF.Property<int[]>(e, nameof(SomeArrayEntity.SomeArray)).Length == 2);

            Assert.Equal(new[] { 3, 4 }, x.SomeArray);
            AssertSql(
                @"SELECT s.""Id"", s.""SomeArray"", s.""SomeByte"", s.""SomeByteArray"", s.""SomeBytea"", s.""SomeMatrix"", s.""SomeStringArray"", s.""SomeText""
FROM ""SomeEntities"" AS s
WHERE cardinality(s.""SomeArray"") = 2
LIMIT 2");
        }

        [Fact]
        public void Length_on_literal_not_translated()
        {
            using var ctx = CreateContext();
            var _ = ctx.SomeEntities.Where(e => new[] { 1, 2, 3 }.Length == e.Id).ToList();

            AssertDoesNotContainInSql("cardinality");
        }

        #endregion

        #region AnyAll

        [Fact]
        public void Any_no_predicate()
        {
            using var ctx = CreateContext();
            var count = ctx.SomeEntities.Count(e => e.SomeArray.Any());

            Assert.Equal(2, count);
            AssertSql(
                @"SELECT COUNT(*)::INT
FROM ""SomeEntities"" AS s
WHERE cardinality(s.""SomeArray"") > 0");
        }

        [Fact]
        public void Any_like()
        {
            using var ctx = CreateContext();
            var _ = ctx.SomeEntities
                .Where(e => new[] { "a%", "b%", "c%" }.Any(p => EF.Functions.Like(e.SomeText, p)))
                .ToList();

            AssertSql(
                @"SELECT s.""Id"", s.""SomeArray"", s.""SomeByte"", s.""SomeByteArray"", s.""SomeBytea"", s.""SomeMatrix"", s.""SomeStringArray"", s.""SomeText""
FROM ""SomeEntities"" AS s
WHERE s.""SomeText"" LIKE ANY (ARRAY['a%','b%','c%']::text[])");
        }

        [Fact]
        public void Any_ilike()
        {
            using var ctx = CreateContext();
            var _ = ctx.SomeEntities
                .Where(e => new[] { "a%", "b%", "c%" }.Any(p => EF.Functions.ILike(e.SomeText, p)))
                .ToList();

            AssertSql(
                @"SELECT s.""Id"", s.""SomeArray"", s.""SomeByte"", s.""SomeByteArray"", s.""SomeBytea"", s.""SomeMatrix"", s.""SomeStringArray"", s.""SomeText""
FROM ""SomeEntities"" AS s
WHERE s.""SomeText"" ILIKE ANY (ARRAY['a%','b%','c%']::text[])");
        }

        [Fact]
        public void Any_like_anonymous()
        {
            using var ctx = CreateContext();
            var patterns = new[] { "a%", "b%", "c%" };

            var anon =
                ctx.SomeEntities
                    .Select(
                        x => new
                        {
                            Array = x.SomeArray,
                            Text = x.SomeText
                        });

            var _ = anon.Where(x => patterns.Any(p => EF.Functions.Like(x.Text, p))).ToList();

            AssertSql(
                @"@__patterns_0='System.String[]' (DbType = Object)

SELECT s.""SomeArray"" AS ""Array"", s.""SomeText"" AS ""Text""
FROM ""SomeEntities"" AS s
WHERE s.""SomeText"" LIKE ANY (@__patterns_0)");
        }

        [Fact]
        public void Any_Contains()
        {
            using var ctx = CreateContext();

            var results = ctx.SomeEntities
                .Where(e => new[] { 2, 3 }.Any(p => e.SomeArray.Contains(p)))
                .ToList();
            Assert.Equal(1, Assert.Single(results).Id);

            results = ctx.SomeEntities
                .Where(e => new[] { 1, 2 }.Any(p => e.SomeArray.Contains(p)))
                .ToList();
            Assert.Empty(results);

            AssertSql(
                @"SELECT s.""Id"", s.""SomeArray"", s.""SomeByte"", s.""SomeByteArray"", s.""SomeBytea"", s.""SomeMatrix"", s.""SomeStringArray"", s.""SomeText""
FROM ""SomeEntities"" AS s
WHERE (ARRAY[2,3]::integer[] && s.""SomeArray"")",
                //
                @"SELECT s.""Id"", s.""SomeArray"", s.""SomeByte"", s.""SomeByteArray"", s.""SomeBytea"", s.""SomeMatrix"", s.""SomeStringArray"", s.""SomeText""
FROM ""SomeEntities"" AS s
WHERE (ARRAY[1,2]::integer[] && s.""SomeArray"")");
        }

        [Fact]
        public void All_Contains()
        {
            using var ctx = CreateContext();

            var results = ctx.SomeEntities
                .Where(e => new[] { 5, 6 }.All(p => e.SomeArray.Contains(p)))
                .ToList();
            Assert.Equal(2, Assert.Single(results).Id);

            results = ctx.SomeEntities
                .Where(e => new[] { 4, 5, 6 }.All(p => e.SomeArray.Contains(p)))
                .ToList();
            Assert.Empty(results);

            AssertSql(
                @"SELECT s.""Id"", s.""SomeArray"", s.""SomeByte"", s.""SomeByteArray"", s.""SomeBytea"", s.""SomeMatrix"", s.""SomeStringArray"", s.""SomeText""
FROM ""SomeEntities"" AS s
WHERE (ARRAY[5,6]::integer[] <@ s.""SomeArray"")",
                //
                @"SELECT s.""Id"", s.""SomeArray"", s.""SomeByte"", s.""SomeByteArray"", s.""SomeBytea"", s.""SomeMatrix"", s.""SomeStringArray"", s.""SomeText""
FROM ""SomeEntities"" AS s
WHERE (ARRAY[4,5,6]::integer[] <@ s.""SomeArray"")");
        }

        #endregion

        #region bytea

        [Fact]
        public void Index_bytea_with_constant()
        {
            using var ctx = CreateContext();
            var actual = ctx.SomeEntities.Where(e => e.SomeBytea[0] == 3).ToList();

            Assert.Single(actual);
            AssertSql(
                @"SELECT s.""Id"", s.""SomeArray"", s.""SomeByte"", s.""SomeByteArray"", s.""SomeBytea"", s.""SomeMatrix"", s.""SomeStringArray"", s.""SomeText""
FROM ""SomeEntities"" AS s
WHERE get_byte(s.""SomeBytea"", 0) = 3");
        }

        [Fact(Skip = "Disabled since EF Core 3.0")]
        public void Index_text_with_constant()
        {
            using var ctx = CreateContext();
            var actual = ctx.SomeEntities.Where(e => e.SomeText[0] == 'f').ToList();

            Assert.Single(actual);
            AssertSql(
                @"SELECT s.""Id"", s.""SomeArray"", s.""SomeByte"", s.""SomeByteArray"", s.""SomeBytea"", s.""SomeMatrix"", s.""SomeStringArray"", s.""SomeText""
FROM ""SomeEntities"" AS s
WHERE (get_byte(s.""SomeBytea"", 0) = 3) AND get_byte(s.""SomeBytea"", 0) IS NOT NULL");
        }

        #endregion

        #region ArrayToString

        [Fact]
        public void String_Join_Array()
        {
            using var ctx = CreateContext();

            var found = ctx.SomeEntities.FirstOrDefault(x => string.Join(" ", x.SomeArray) == "3 4");
            var found2 = ctx.SomeEntities.FirstOrDefault(x => string.Join(' ', x.SomeArray) == "3 4");

            Assert.NotNull(found);
            Assert.True(found == found2);
            AssertSql(
                @"SELECT s.""Id"", s.""SomeArray"", s.""SomeByte"", s.""SomeByteArray"", s.""SomeBytea"", s.""SomeMatrix"", s.""SomeStringArray"", s.""SomeText""
FROM ""SomeEntities"" AS s
WHERE array_to_string(s.""SomeArray"", ' ', '') = '3 4'
LIMIT 1",
                //
                @"SELECT s.""Id"", s.""SomeArray"", s.""SomeByte"", s.""SomeByteArray"", s.""SomeBytea"", s.""SomeMatrix"", s.""SomeStringArray"", s.""SomeText""
FROM ""SomeEntities"" AS s
WHERE array_to_string(s.""SomeArray"", ' ', '') = '3 4'
LIMIT 1");
        }

        [Fact]
        public void String_Join_StringArray()
        {
            using var ctx = CreateContext();

            var found = ctx.SomeEntities.FirstOrDefault(x => string.Join(" ", x.SomeStringArray) == "one two three");
            var found2 = ctx.SomeEntities.FirstOrDefault(x => string.Join(' ', x.SomeStringArray) == "one two three");

            Assert.NotNull(found);
            Assert.True(found == found2);
            AssertSql(
                @"SELECT s.""Id"", s.""SomeArray"", s.""SomeByte"", s.""SomeByteArray"", s.""SomeBytea"", s.""SomeMatrix"", s.""SomeStringArray"", s.""SomeText""
FROM ""SomeEntities"" AS s
WHERE array_to_string(s.""SomeStringArray"", ' ', '') = 'one two three'
LIMIT 1",
                //
                @"SELECT s.""Id"", s.""SomeArray"", s.""SomeByte"", s.""SomeByteArray"", s.""SomeBytea"", s.""SomeMatrix"", s.""SomeStringArray"", s.""SomeText""
FROM ""SomeEntities"" AS s
WHERE array_to_string(s.""SomeStringArray"", ' ', '') = 'one two three'
LIMIT 1");
        }

        [Fact]
        public void String_Join_Array_with_nulls()
        {
            using var ctx = CreateContext();

            var found = ctx.SomeEntities.FirstOrDefault(x => string.Join(",", x.SomeStringArray) == "foo,,bar");
            var found2 = ctx.SomeEntities.FirstOrDefault(x => string.Join("", x.SomeStringArray) == "foobar");

            Assert.NotNull(found);
            Assert.NotNull(found2);
            AssertSql(
                @"SELECT s.""Id"", s.""SomeArray"", s.""SomeByte"", s.""SomeByteArray"", s.""SomeBytea"", s.""SomeMatrix"", s.""SomeStringArray"", s.""SomeText""
FROM ""SomeEntities"" AS s
WHERE array_to_string(s.""SomeStringArray"", ',', '') = 'foo,,bar'
LIMIT 1",
                //
                @"SELECT s.""Id"", s.""SomeArray"", s.""SomeByte"", s.""SomeByteArray"", s.""SomeBytea"", s.""SomeMatrix"", s.""SomeStringArray"", s.""SomeText""
FROM ""SomeEntities"" AS s
WHERE array_to_string(s.""SomeStringArray"", '', '') = 'foobar'
LIMIT 1");
        }

        [Fact]
        public void String_Join_null_array()
        {
            using var ctx = CreateContext();

            var found = ctx.SomeEntities.FirstOrDefault(x => string.Join(",", x.SomeArray) == null);

            Assert.NotNull(found);
            Assert.Equal(3, found.Id);
            AssertSql(
                @"SELECT s.""Id"", s.""SomeArray"", s.""SomeByte"", s.""SomeByteArray"", s.""SomeBytea"", s.""SomeMatrix"", s.""SomeStringArray"", s.""SomeText""
FROM ""SomeEntities"" AS s
WHERE (s.""SomeArray"" IS NULL)
LIMIT 1");
        }

        [Fact]
        public void String_Join_with_constants()
        {
            using var ctx = CreateContext();

            var found = ctx.SomeEntities.FirstOrDefault(x =>
                string.Join(",", new int[] { 1, 2, 3 }) == "1,2,3" && x.Id == 1);

            Assert.NotNull(found);
            Assert.Equal(1, found.Id);
            AssertSql(
                @"SELECT s.""Id"", s.""SomeArray"", s.""SomeByte"", s.""SomeByteArray"", s.""SomeBytea"", s.""SomeMatrix"", s.""SomeStringArray"", s.""SomeText""
FROM ""SomeEntities"" AS s
WHERE s.""Id"" = 1
LIMIT 1");
        }

        [Fact]
        public void String_Join_with_string_array_literal()
        {
            using var ctx = CreateContext();

            var found = ctx.SomeEntities.FirstOrDefault(x =>
                string.Join(x.SomeText, new string[] { "foo", "bar" }) == "foofoobar");

            Assert.NotNull(found);
            Assert.Equal(1, found.Id);
            AssertSql(
                @"SELECT s.""Id"", s.""SomeArray"", s.""SomeByte"", s.""SomeByteArray"", s.""SomeBytea"", s.""SomeMatrix"", s.""SomeStringArray"", s.""SomeText""
FROM ""SomeEntities"" AS s
WHERE array_to_string(ARRAY['foo','bar']::text[], s.""SomeText"", '') = 'foofoobar'
LIMIT 1");
        }

        [Fact]
        public void String_Join_with_int_array_literal()
        {
            using var ctx = CreateContext();

            var found = ctx.SomeEntities.FirstOrDefault(x =>
                string.Join(x.SomeText, new int[] { 3, 4 }) == "3foo4");

            Assert.NotNull(found);
            Assert.Equal(1, found.Id);
            AssertSql(
                @"SELECT s.""Id"", s.""SomeArray"", s.""SomeByte"", s.""SomeByteArray"", s.""SomeBytea"", s.""SomeMatrix"", s.""SomeStringArray"", s.""SomeText""
FROM ""SomeEntities"" AS s
WHERE array_to_string(ARRAY[3,4]::integer[], s.""SomeText"", '') = '3foo4'
LIMIT 1");
        }

        [Fact]
        public void String_Join_String_throws_exception()
        {
            using var ctx = CreateContext();

            Assert.Throws<InvalidOperationException>(() =>
                ctx.SomeEntities.FirstOrDefault(x => string.Join(x.SomeText, (IEnumerable<char>)"foobar") == null));
        }

        #endregion

        #region Support

        protected ArrayArrayQueryContext CreateContext() => Fixture.CreateContext();

        void AssertSql(params string[] expected)
            => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);

        void AssertDoesNotContainInSql(string expected)
            => Assert.DoesNotContain(expected, Fixture.TestSqlLoggerFactory.Sql);

        public class ArrayArrayQueryContext : PoolableDbContext
        {
            public DbSet<SomeArrayEntity> SomeEntities { get; set; }

            public ArrayArrayQueryContext(DbContextOptions options) : base(options) {}

            public static void Seed(ArrayArrayQueryContext context)
            {
                context.SomeEntities.AddRange(
                    new SomeArrayEntity
                    {
                        Id = 1,
                        SomeArray = new[] { 3, 4 },
                        SomeStringArray = new[] { "foo", null, "bar" },
                        SomeBytea = new byte[] { 3, 4 },
                        SomeByteArray = new byte[] { 3, 4 },
                        SomeMatrix = new[,] { { 5, 6 }, { 7, 8 } },
                        SomeText = "foo",
                        SomeByte = 10
                    },
                    new SomeArrayEntity
                    {
                        Id = 2,
                        SomeArray = new[] { 5, 6, 7 },
                        SomeStringArray = new[] { "one", "two", "three" },
                        SomeBytea = new byte[] { 5, 6, 7 },
                        SomeByteArray = new byte[] { 5, 6, 7 },
                        SomeMatrix = new[,] { { 10, 11 }, { 12, 13 } },
                        SomeText = "bar",
                        SomeByte = 20
                    },
                    new SomeArrayEntity
                    {
                        Id = 3,
                        SomeArray = null,
                        SomeStringArray = null,
                        SomeBytea = new byte[] { 1, 2, 3 },
                        SomeByteArray = new byte[] { 1, 2, 3 },
                        SomeMatrix = new[,] { { 1, 2 }, { 3, 4 } },
                        SomeText = "abc",
                        SomeByte = 30
                    });
                context.SaveChanges();
            }
        }

        public class SomeArrayEntity
        {
            public int Id { get; set; }
            public int[] SomeArray { get; set; }
            public string[] SomeStringArray { get; set; }
            public int[,] SomeMatrix { get; set; }
            public byte[] SomeBytea { get; set; }
            public byte[] SomeByteArray { get; set; }
            public string SomeText { get; set; }
            public byte SomeByte { get; set; }
        }

        public class ArrayArrayQueryFixture : SharedStoreFixtureBase<ArrayArrayQueryContext>
        {
            protected override string StoreName => "ArrayArrayQueryTest";
            protected override ITestStoreFactory TestStoreFactory => NpgsqlTestStoreFactory.Instance;
            public TestSqlLoggerFactory TestSqlLoggerFactory => (TestSqlLoggerFactory)ListLoggerFactory;
            protected override void Seed(ArrayArrayQueryContext context) => ArrayArrayQueryContext.Seed(context);
        }

        #endregion
    }
}
