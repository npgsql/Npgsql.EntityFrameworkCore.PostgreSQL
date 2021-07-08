using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestModels.Northwind;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Npgsql.EntityFrameworkCore.PostgreSQL.TestUtilities;
using Xunit;
using Xunit.Abstractions;

namespace Npgsql.EntityFrameworkCore.PostgreSQL.Query
{
    public class NorthwindFunctionsQueryNpgsqlTest : NorthwindFunctionsQueryRelationalTestBase<NorthwindQueryNpgsqlFixture<NoopModelCustomizer>>
    {
        // ReSharper disable once UnusedParameter.Local
        public NorthwindFunctionsQueryNpgsqlTest(
            NorthwindQueryNpgsqlFixture<NoopModelCustomizer> fixture, ITestOutputHelper testOutputHelper)
            : base(fixture)
        {
            ClearLog();
            //Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
        }
        public override async Task IsNullOrWhiteSpace_in_predicate(bool async)
        {
            await base.IsNullOrWhiteSpace_in_predicate(async);

            AssertSql(
                @"SELECT c.""CustomerID"", c.""Address"", c.""City"", c.""CompanyName"", c.""ContactName"", c.""ContactTitle"", c.""Country"", c.""Fax"", c.""Phone"", c.""PostalCode"", c.""Region""
FROM ""Customers"" AS c
WHERE (c.""Region"" IS NULL) OR (btrim(c.""Region"", E' \t\n\r') = '')");
        }

        public override Task Where_math_log_new_base(bool async)
            => Task.CompletedTask; // PostgreSQL only has log(x, base) over numeric, may be possible to cast back and forth though

        public override Task Convert_ToString(bool async)
            => Task.CompletedTask; // Convert on DateTime not yet supported

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public virtual async Task Convert_ToDateTime(bool async)
        {
            await AssertQuery(
                async,
                ss => ss.Set<Order>().Where(o => o.OrderDate == Convert.ToDateTime(o.OrderDate.ToString())),
                entryCount: 830);

            AssertContainsSqlFragment(@"WHERE (o.""OrderDate"" = CAST(CAST(o.""OrderDate"" AS text) AS timestamp without time zone)) OR (o.""OrderDate"" IS NULL)");
        }

        #region Substring

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public Task Substring_without_length_with_Index_of(bool async)
            => AssertQuery(
                async,
                ss => ss.Set<Customer>()
                    .Where(x => x.Address == "Walserweg 21")
                    .Where(x => x.Address.Substring(x.Address.IndexOf("e")) == "erweg 21"),
                entryCount: 1);

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public Task Substring_without_length_with_constant(bool async)
            => AssertQuery(
                async,
                //Walserweg 21
                cs => cs.Set<Customer>().Where(x => x.Address.Substring(5) == "rweg 21"),
                entryCount: 1);

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public Task Substring_without_length_with_closure(bool async)
        {
            var startIndex = 5;
            return AssertQuery(
                async,
                //Walserweg 21
                ss => ss.Set<Customer>().Where(x => x.Address.Substring(startIndex) == "rweg 21"),
                entryCount: 1);
        }

        #endregion

        #region Regex

        [Theory]
        [MemberData(nameof(IsAsyncData))]
        public async Task Regex_IsMatch(bool async)
        {
            await AssertQuery(
                async,
                cs => cs.Set<Customer>().Where(c => Regex.IsMatch(c.CompanyName, "^A")),
                entryCount: 4);

            AssertContainsSqlFragment(@"WHERE c.""CompanyName"" ~ ('(?p)' || '^A')");
        }

        [Theory]
        [MemberData(nameof(IsAsyncData))]
        public async Task Regex_IsMatchOptionsNone(bool async)
        {
            await AssertQuery(
                async,
                cs => cs.Set<Customer>().Where(c => Regex.IsMatch(c.CompanyName, "^A", RegexOptions.None)),
                entryCount: 4);

            AssertContainsSqlFragment(@"WHERE c.""CompanyName"" ~ ('(?p)' || '^A')");
        }

        [Theory]
        [MemberData(nameof(IsAsyncData))]
        public async Task Regex_IsMatchOptionsIgnoreCase(bool async)
        {
            await AssertQuery(
                async,
                cs => cs.Set<Customer>().Where(c => Regex.IsMatch(c.CompanyName, "^a", RegexOptions.IgnoreCase)),
                entryCount: 4);

            AssertContainsSqlFragment(@"WHERE c.""CompanyName"" ~ ('(?ip)' || '^a')");
        }

        [Theory]
        [MemberData(nameof(IsAsyncData))]
        public async Task Regex_IsMatchOptionsMultiline(bool async)
        {
            await AssertQuery(
                async,
                cs => cs.Set<Customer>().Where(c => Regex.IsMatch(c.CompanyName, "^A", RegexOptions.Multiline)),
                entryCount: 4);

            AssertContainsSqlFragment(@"WHERE c.""CompanyName"" ~ ('(?n)' || '^A')");
        }

        // ReSharper disable once IdentifierTypo
        [Theory]
        [MemberData(nameof(IsAsyncData))]
        public async Task Regex_IsMatchOptionsSingleline(bool async)
        {
            await AssertQuery(
                async,
                cs => cs.Set<Customer>().Where(c => Regex.IsMatch(c.CompanyName, "^A", RegexOptions.Singleline)),
                entryCount: 4);

            AssertContainsSqlFragment(@"WHERE c.""CompanyName"" ~ '^A'");
        }

        [Theory]
        [MemberData(nameof(IsAsyncData))]
        public async Task Regex_IsMatchOptionsIgnorePatternWhitespace(bool async)
        {
            await AssertQuery(
                async,
                cs => cs.Set<Customer>().Where(c => Regex.IsMatch(c.CompanyName, "^ A", RegexOptions.IgnorePatternWhitespace)),
                entryCount: 4);

            AssertContainsSqlFragment(@"WHERE c.""CompanyName"" ~ ('(?px)' || '^ A')");
        }

        [Fact]
        public void Regex_IsMatchOptionsUnsupported()
            => Assert.Throws<InvalidOperationException>(() =>
                Fixture.CreateContext().Customers.Where(c => Regex.IsMatch(c.CompanyName, "^A", RegexOptions.RightToLeft)).ToList());

        #endregion Regex

        #region Guid

        private static string UuidGenerationFunction { get; } = TestEnvironment.PostgresVersion.AtLeast(13)
            ? "gen_random_uuid"
            : "uuid_generate_v4";

        public override async Task Where_guid_newguid(bool async)
        {
            await base.Where_guid_newguid(async);

            AssertSql(
                @$"SELECT c.""CustomerID"", c.""Address"", c.""City"", c.""CompanyName"", c.""ContactName"", c.""ContactTitle"", c.""Country"", c.""Fax"", c.""Phone"", c.""PostalCode"", c.""Region""
FROM ""Customers"" AS c
WHERE {UuidGenerationFunction}() <> '00000000-0000-0000-0000-000000000000'");
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public virtual async Task OrderBy_Guid_NewGuid(bool async)
        {
            await AssertQuery(
                async,
                ods => ods.Set<OrderDetail>().OrderBy(od => Guid.NewGuid()).Select(x => x),
                entryCount: 2155);

            AssertSql(
                @$"SELECT o.""OrderID"", o.""ProductID"", o.""Discount"", o.""Quantity"", o.""UnitPrice""
FROM ""Order Details"" AS o
ORDER BY {UuidGenerationFunction}() NULLS FIRST");
        }

        #endregion

        #region PadLeft, PadRight

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public Task PadLeft_with_constant(bool async)
            => AssertQuery(
                async,
                ss => ss.Set<Customer>().Where(x => x.Address.PadLeft(20).EndsWith("Walserweg 21")),
                entryCount: 1);

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public Task PadLeft_char_with_constant(bool async)
            => AssertQuery(
                async,
                ss => ss.Set<Customer>().Where(x => x.Address.PadLeft(20, 'a').EndsWith("Walserweg 21")),
                entryCount: 1);

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public Task PadLeft_with_parameter(bool async)
        {
            var length = 20;

            return AssertQuery(
                async,
                ss => ss.Set<Customer>().Where(x => x.Address.PadLeft(length).EndsWith("Walserweg 21")),
                entryCount: 1);
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public Task PadLeft_char_with_parameter(bool async)
        {
            var length = 20;

            return AssertQuery(
                async,
                ss => ss.Set<Customer>().Where(x => x.Address.PadLeft(length, 'a').EndsWith("Walserweg 21")),
                entryCount: 1);
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public Task PadRight_with_constant(bool async)
            => AssertQuery(
                async,
                ss => ss.Set<Customer>().Where(x => x.Address.PadRight(20).StartsWith("Walserweg 21")),
                entryCount: 1);

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public Task PadRight_char_with_constant(bool async)
            => AssertQuery(
                async,
                ss => ss.Set<Customer>().Where(x => x.Address.PadRight(20).StartsWith("Walserweg 21")),
                entryCount: 1);

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public Task PadRight_with_parameter(bool async)
        {
            var length = 20;

            return AssertQuery(
                async,
                ss => ss.Set<Customer>().Where(x => x.Address.PadRight(length).StartsWith("Walserweg 21")),
                entryCount: 1);
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public Task PadRight_char_with_parameter(bool async)
        {
            var length = 20;

            return AssertQuery(
                async,
                ss => ss.Set<Customer>().Where(x => x.Address.PadRight(length, 'a').StartsWith("Walserweg 21")),
                entryCount: 1);
        }

        #endregion

        #region Unsupported

        // These tests convert (among other things) to and from boolean, which PostgreSQL
        // does not support (https://github.com/dotnet/efcore/issues/19606)
        public override Task Convert_ToBoolean(bool async) => Task.CompletedTask;
        public override Task Convert_ToByte(bool async) => Task.CompletedTask;
        public override Task Convert_ToDecimal(bool async) => Task.CompletedTask;
        public override Task Convert_ToDouble(bool async) => Task.CompletedTask;
        public override Task Convert_ToInt16(bool async) => Task.CompletedTask;
        public override Task Convert_ToInt64(bool async) => Task.CompletedTask;

        #endregion Unsupported

        private void AssertSql(params string[] expected)
            => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);

        protected override void ClearLog()
            => Fixture.TestSqlLoggerFactory.Clear();

        private void AssertContainsSqlFragment(string expectedFragment)
            => Assert.Contains(Fixture.TestSqlLoggerFactory.SqlStatements, s => s.Contains(expectedFragment));
    }
}
