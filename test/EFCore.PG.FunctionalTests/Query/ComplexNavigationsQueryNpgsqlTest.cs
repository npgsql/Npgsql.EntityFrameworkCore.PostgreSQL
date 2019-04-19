﻿using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestUtilities.Xunit;
using Xunit;

namespace Npgsql.EntityFrameworkCore.PostgreSQL.Query
{
    public class ComplexNavigationsQueryNpgsqlTest
        : ComplexNavigationsQueryTestBase<ComplexNavigationsQueryNpgsqlFixture>
    {
        public ComplexNavigationsQueryNpgsqlTest(ComplexNavigationsQueryNpgsqlFixture fixture)
            : base(fixture)
        {
            Fixture.TestSqlLoggerFactory.Clear();
        }

        [ConditionalTheory(Skip = "https://github.com/aspnet/EntityFrameworkCore/pull/12970")]
        [MemberData(nameof(IsAsyncData))]
        public override Task Null_check_in_anonymous_type_projection_should_not_be_removed(bool isAsync) => null;

        [ConditionalTheory(Skip = "Disabled in EFCore, https://github.com/aspnet/EntityFrameworkCore/issues/15064")]
        public override Task Include_reference_collection_order_by_reference_navigation(bool isAsync) => null;

    }
}
