using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestModels.Northwind;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Xunit;
using Xunit.Abstractions;

namespace Npgsql.EntityFrameworkCore.PostgreSQL.Query
{
    public partial class SimpleQueryNpgsqlTest : SimpleQueryTestBase<NorthwindQueryNpgsqlFixture<NoopModelCustomizer>>
    {
        public SimpleQueryNpgsqlTest(NorthwindQueryNpgsqlFixture<NoopModelCustomizer> fixture, ITestOutputHelper testOutputHelper)
            : base(fixture)
        {
            Fixture.TestSqlLoggerFactory.Clear();
            //Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
        }

        #region Overrides

        public override async Task Select_expression_date_add_year(bool isAsync)
        {
            await base.Select_expression_date_add_year(isAsync);

            AssertSql(
                @"SELECT o.""OrderDate"" + INTERVAL '1 years' AS ""OrderDate""
FROM ""Orders"" AS o
WHERE (o.""OrderDate"" IS NOT NULL)");
        }

        [Theory]
        [MemberData(nameof(IsAsyncData))]
        public async Task Select_expression_date_add_year_param(bool isAsync)
        {
            var years = 2;

            await AssertQuery(
                isAsync,
                ss => ss.Set<Order>().Where(o => o.OrderDate != null)
                    .Select(
                        o => new Order
                        {
                            OrderDate = o.OrderDate.Value.AddYears(years)
                        }),
                e => e.OrderDate);

            AssertSql(
                @"@__years_0='2'

SELECT o.""OrderDate"" + CAST((@__years_0 || ' years') AS interval) AS ""OrderDate""
FROM ""Orders"" AS o
WHERE (o.""OrderDate"" IS NOT NULL)");
        }

        public override async Task Contains_with_local_uint_array_closure(bool isAsync)
        {
            await base.Contains_with_local_uint_array_closure(isAsync);

            // Note: PostgreSQL doesn't support uint, but value converters make this into bigint
            AssertSql(
                @"@__ids_0='System.Int64[]' (DbType = Object)

SELECT e.""EmployeeID"", e.""City"", e.""Country"", e.""FirstName"", e.""ReportsTo"", e.""Title""
FROM ""Employees"" AS e
WHERE e.""EmployeeID"" = ANY (@__ids_0)",
                //
                @"@__ids_0='System.Int64[]' (DbType = Object)

SELECT e.""EmployeeID"", e.""City"", e.""Country"", e.""FirstName"", e.""ReportsTo"", e.""Title""
FROM ""Employees"" AS e
WHERE e.""EmployeeID"" = ANY (@__ids_0)");
        }

        public override async Task Contains_with_local_nullable_uint_array_closure(bool isAsync)
        {
            await base.Contains_with_local_nullable_uint_array_closure(isAsync);

            // Note: PostgreSQL doesn't support uint, but value converters make this into bigint

            AssertSql(
                @"SELECT e.""EmployeeID"", e.""City"", e.""Country"", e.""FirstName"", e.""ReportsTo"", e.""Title""
FROM ""Employees"" AS e
WHERE e.""EmployeeID"" IN (0, 1)",
                //
                @"SELECT e.""EmployeeID"", e.""City"", e.""Country"", e.""FirstName"", e.""ReportsTo"", e.""Title""
FROM ""Employees"" AS e
WHERE e.""EmployeeID"" IN (0)");
        }

        #endregion

        #region PadLeft, PadRight

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public Task PadLeft_with_constant(bool isAsync)
            => AssertQuery(
                isAsync,
                ss => ss.Set<Customer>().Where(x => x.Address.PadLeft(20).EndsWith("Walserweg 21")),
                entryCount: 1);

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public Task PadLeft_char_with_constant(bool isAsync)
            => AssertQuery(
                isAsync,
                ss => ss.Set<Customer>().Where(x => x.Address.PadLeft(20, 'a').EndsWith("Walserweg 21")),
                entryCount: 1);

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public Task PadLeft_with_parameter(bool isAsync)
        {
            var length = 20;

            return AssertQuery(
                isAsync,
                ss => ss.Set<Customer>().Where(x => x.Address.PadLeft(length).EndsWith("Walserweg 21")),
                entryCount: 1);
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public Task PadLeft_char_with_parameter(bool isAsync)
        {
            var length = 20;

            return AssertQuery(
                isAsync,
                ss => ss.Set<Customer>().Where(x => x.Address.PadLeft(length, 'a').EndsWith("Walserweg 21")),
                entryCount: 1);
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public Task PadRight_with_constant(bool isAsync)
            => AssertQuery(
                isAsync,
                ss => ss.Set<Customer>().Where(x => x.Address.PadRight(20).StartsWith("Walserweg 21")),
                entryCount: 1);

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public Task PadRight_char_with_constant(bool isAsync)
            => AssertQuery(
                isAsync,
                ss => ss.Set<Customer>().Where(x => x.Address.PadRight(20).StartsWith("Walserweg 21")),
                entryCount: 1);

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public Task PadRight_with_parameter(bool isAsync)
        {
            var length = 20;

            return AssertQuery(
                isAsync,
                ss => ss.Set<Customer>().Where(x => x.Address.PadRight(length).StartsWith("Walserweg 21")),
                entryCount: 1);
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public Task PadRight_char_with_parameter(bool isAsync)
        {
            var length = 20;

            return AssertQuery(
                isAsync,
                ss => ss.Set<Customer>().Where(x => x.Address.PadRight(length, 'a').StartsWith("Walserweg 21")),
                entryCount: 1);
        }

        #endregion

        #region Substring

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public Task Substring_without_length_with_Index_of(bool isAsync)
            => AssertQuery(
                isAsync,
                ss => ss.Set<Customer>()
                    .Where(x => x.Address == "Walserweg 21")
                    .Where(x => x.Address.Substring(x.Address.IndexOf("e")) == "erweg 21"),
                entryCount: 1);

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public Task Substring_without_length_with_constant(bool isAsync)
            => AssertQuery(
                isAsync,
                //Walserweg 21
                cs => cs.Set<Customer>().Where(x => x.Address.Substring(5) == "rweg 21"),
                entryCount: 1);

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public Task Substring_without_length_with_closure(bool isAsync)
        {
            var startIndex = 5;
            return AssertQuery(
                isAsync,
                //Walserweg 21
                ss => ss.Set<Customer>().Where(x => x.Address.Substring(startIndex) == "rweg 21"),
                entryCount: 1);
        }

        #endregion

        #region Array contains

        // Note that this also takes care of array.Any(x => x == y)
        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public async Task Array_Contains_constant(bool isAsync)
        {
            await AssertQuery(
                isAsync,
                ss => ss.Set<Customer>().Where(c => new[] { "ALFKI", "ANATR" }.Contains(c.CustomerID)),
                entryCount: 2);

            // Note: for constant lists there's no advantage in using the PostgreSQL-specific x = ANY (a b, c), unlike
            // for parameterized lists.

            AssertSql(
                @"SELECT c.""CustomerID"", c.""Address"", c.""City"", c.""CompanyName"", c.""ContactName"", c.""ContactTitle"", c.""Country"", c.""Fax"", c.""Phone"", c.""PostalCode"", c.""Region""
FROM ""Customers"" AS c
WHERE c.""CustomerID"" IN ('ALFKI', 'ANATR')");
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public async Task Array_Contains_parameter(bool isAsync)
        {
            var regions = new[] { "UK", "SP" };

            await AssertQuery(
                isAsync,
                ss => ss.Set<Customer>().Where(c => regions.Contains(c.Region)),
                entryCount: 6);

            // Instead of c.""Region"" IN ('UK', 'SP') we generate the PostgreSQL-specific x = ANY (a, b, c), which can
            // be parameterized.
            // Ideally parameter sniffing would allow us to produce SQL without the null check since the regions array doesn't contain one
            // (see https://github.com/aspnet/EntityFrameworkCore/issues/17598).
            AssertSql(
                @"@__regions_0='System.String[]' (DbType = Object)

SELECT c.""CustomerID"", c.""Address"", c.""City"", c.""CompanyName"", c.""ContactName"", c.""ContactTitle"", c.""Country"", c.""Fax"", c.""Phone"", c.""PostalCode"", c.""Region""
FROM ""Customers"" AS c
WHERE c.""Region"" = ANY (@__regions_0) OR ((c.""Region"" IS NULL) AND (array_position(@__regions_0, NULL) IS NOT NULL))");
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public async Task Array_Contains_parameter_with_null(bool isAsync)
        {
            var regions = new[] { "UK", "SP", null };

            await AssertQuery(
            isAsync,
            ss => ss.Set<Customer>().Where(c => regions.Contains(c.Region)),
            entryCount: 66);

            // Instead of c.""Region"" IN ('UK', 'SP') we generate the PostgreSQL-specific x = ANY (a, b, c), which can
            // be parameterized.
            // Ideally parameter sniffing would allow us to produce SQL with an optimized null check (no need to check the array server-side)
            // (see https://github.com/aspnet/EntityFrameworkCore/issues/17598).
            AssertSql(
                @"@__regions_0='System.String[]' (DbType = Object)

SELECT c.""CustomerID"", c.""Address"", c.""City"", c.""CompanyName"", c.""ContactName"", c.""ContactTitle"", c.""Country"", c.""Fax"", c.""Phone"", c.""PostalCode"", c.""Region""
FROM ""Customers"" AS c
WHERE c.""Region"" = ANY (@__regions_0) OR ((c.""Region"" IS NULL) AND (array_position(@__regions_0, NULL) IS NOT NULL))");
        }

        #endregion Array contains

        #region Any/All Like

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public async Task Array_Any_Like(bool isAsync)
        {
            var collection = new[] { "A%", "B%", "C%" };

            await AssertQuery(
                isAsync,
                ss => ss.Set<Customer>().Where(c => collection.Any(y => EF.Functions.Like(c.Address, y))),
                entryCount: 22);

            AssertSql(
                @"@__collection_0='System.String[]' (DbType = Object)

SELECT c.""CustomerID"", c.""Address"", c.""City"", c.""CompanyName"", c.""ContactName"", c.""ContactTitle"", c.""Country"", c.""Fax"", c.""Phone"", c.""PostalCode"", c.""Region""
FROM ""Customers"" AS c
WHERE c.""Address"" LIKE ANY (@__collection_0)");
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public async Task Array_All_Like(bool isAsync)
        {
            var collection = new[] { "A%", "B%", "C%" };

            await AssertQuery(
                isAsync,
                ss => ss.Set<Customer>().Where(c => collection.All(y => EF.Functions.Like(c.Address, y))));

            AssertSql(
                @"@__collection_0='System.String[]' (DbType = Object)

SELECT c.""CustomerID"", c.""Address"", c.""City"", c.""CompanyName"", c.""ContactName"", c.""ContactTitle"", c.""Country"", c.""Fax"", c.""Phone"", c.""PostalCode"", c.""Region""
FROM ""Customers"" AS c
WHERE c.""Address"" LIKE ALL (@__collection_0)");
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public async Task Array_Any_ILike(bool isAsync)
        {
            var collection = new[] { "a%", "b%", "c%" };

            await AssertQuery(
                isAsync,
                ss => ss.Set<Customer>().Where(c => collection.Any(y => EF.Functions.ILike(c.Address, y))),
                entryCount: 22);

            AssertSql(
                @"@__collection_0='System.String[]' (DbType = Object)

SELECT c.""CustomerID"", c.""Address"", c.""City"", c.""CompanyName"", c.""ContactName"", c.""ContactTitle"", c.""Country"", c.""Fax"", c.""Phone"", c.""PostalCode"", c.""Region""
FROM ""Customers"" AS c
WHERE c.""Address"" ILIKE ANY (@__collection_0)");
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public async Task Array_All_ILike(bool isAsync)
        {
            var collection = new[] { "a%", "b%", "c%" };

            await AssertQuery(
                isAsync,
                ss => ss.Set<Customer>().Where(c => collection.All(y => EF.Functions.ILike(c.Address, y))));

            AssertSql(
                @"@__collection_0='System.String[]' (DbType = Object)

SELECT c.""CustomerID"", c.""Address"", c.""City"", c.""CompanyName"", c.""ContactName"", c.""ContactTitle"", c.""Country"", c.""Fax"", c.""Phone"", c.""PostalCode"", c.""Region""
FROM ""Customers"" AS c
WHERE c.""Address"" ILIKE ALL (@__collection_0)");
        }

        #endregion Any/All Like

        [ConditionalFact] // #1560
        public async Task Lateral_join_with_table_is_rewritten_with_subquery()
        {
            await using var ctx = CreateContext();

            _ = await ctx.Customers.Select(c1 => ctx.Customers.Select(c2 => c2.ContactName).ToList()).ToListAsync();

            AssertSql(
                @"SELECT c.""CustomerID"", c0.""ContactName"", c0.""CustomerID""
FROM ""Customers"" AS c
LEFT JOIN LATERAL (SELECT * FROM ""Customers"") AS c0 ON TRUE
ORDER BY c.""CustomerID"" NULLS FIRST, c0.""CustomerID"" NULLS FIRST");
        }

        #region Helpers

        void AssertSql(params string[] expected)
            => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);

        #endregion
    }
}
