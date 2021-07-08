using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Scaffolding.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.TestUtilities;
using NpgsqlTypes;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Npgsql.EntityFrameworkCore.PostgreSQL.Migrations
{
    public class MigrationsNpgsqlTest : MigrationsTestBase<MigrationsNpgsqlTest.MigrationsNpgsqlFixture>
    {
        public MigrationsNpgsqlTest(MigrationsNpgsqlFixture fixture, ITestOutputHelper testOutputHelper)
            : base(fixture)
        {
            Fixture.TestSqlLoggerFactory.Clear();
            //Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
        }

        #region Table

        public override async Task Create_table()
        {
            await base.Create_table();

            AssertSql(
                @"CREATE TABLE ""People"" (
    ""Id"" integer NOT NULL,
    ""Name"" text NULL,
    CONSTRAINT ""PK_People"" PRIMARY KEY (""Id"")
);");
        }

        [Fact]
        public override async Task Create_table_all_settings()
        {
            await base.Create_table_all_settings();

            AssertSql(
                @"CREATE SCHEMA IF NOT EXISTS dbo2;",
                //
                @"CREATE TABLE dbo2.""People"" (
    ""CustomId"" integer NOT NULL,
    ""EmployerId"" integer NOT NULL,
    ""SSN"" character varying(11) COLLATE ""POSIX"" NOT NULL,
    CONSTRAINT ""PK_People"" PRIMARY KEY (""CustomId""),
    CONSTRAINT ""AK_People_SSN"" UNIQUE (""SSN""),
    CONSTRAINT ""CK_EmployerId"" CHECK (""EmployerId"" > 0),
    CONSTRAINT ""FK_People_Employers_EmployerId"" FOREIGN KEY (""EmployerId"") REFERENCES ""Employers"" (""Id"") ON DELETE RESTRICT
);
COMMENT ON TABLE dbo2.""People"" IS 'Table comment';
COMMENT ON COLUMN dbo2.""People"".""EmployerId"" IS 'Employer ID comment';");
        }

        public override async Task Create_table_no_key()
        {
            await base.Create_table_no_key();

            AssertSql(
                @"CREATE TABLE ""Anonymous"" (
    ""SomeColumn"" integer NOT NULL
);");
        }

        public override async Task Create_table_with_comments()
        {
            await base.Create_table_with_comments();

            AssertSql(
                @"CREATE TABLE ""People"" (
    ""Id"" integer NOT NULL,
    ""Name"" text NULL
);
COMMENT ON TABLE ""People"" IS 'Table comment';
COMMENT ON COLUMN ""People"".""Name"" IS 'Column comment';");
        }

        public override async Task Create_table_with_multiline_comments()
        {
            await base.Create_table_with_multiline_comments();

            AssertSql(
                @"CREATE TABLE ""People"" (
    ""Id"" integer NOT NULL,
    ""Name"" text NULL
);
COMMENT ON TABLE ""People"" IS 'This is a multi-line
table comment.
More information can
be found in the docs.';
COMMENT ON COLUMN ""People"".""Name"" IS 'This is a multi-line
column comment.
More information can
be found in the docs.';");
        }

        public override async Task Create_table_with_computed_column(bool? stored)
        {
            if (TestEnvironment.PostgresVersion.IsUnder(12))
            {
                await Assert.ThrowsAsync<NotSupportedException>(() => base.Create_table_with_computed_column(stored));
                return;
            }

            if (stored != true)
            {
                // Non-stored generated columns aren't yet supported (PG12)
                await Assert.ThrowsAsync<NotSupportedException>(() => base.Create_table_with_computed_column(stored));
                return;
            }

            await base.Create_table_with_computed_column(stored: true);

            AssertSql(
                @"CREATE TABLE ""People"" (
    ""Id"" integer NOT NULL,
    ""Sum"" text GENERATED ALWAYS AS (""X"" + ""Y"") STORED,
    ""X"" integer NOT NULL,
    ""Y"" integer NOT NULL
);");
        }

        [Fact]
        public virtual async Task Create_table_with_identity_by_default()
        {
            await Test(
                builder => { },
                builder => builder.Entity("People").Property<int>("Id")
                    .UseIdentityByDefaultColumn(),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    var column = Assert.Single(table.Columns);
                    Assert.Equal(NpgsqlValueGenerationStrategy.IdentityByDefaultColumn, column[NpgsqlAnnotationNames.ValueGenerationStrategy]);
                    Assert.Null(column[NpgsqlAnnotationNames.IdentityOptions]);
                });

            AssertSql(
                @"CREATE TABLE ""People"" (
    ""Id"" integer GENERATED BY DEFAULT AS IDENTITY
);");
        }

        [Fact]
        public virtual async Task Create_table_with_identity_always()
        {
            await Test(
                builder => { },
                builder => builder.Entity("People").Property<int>("Id")
                    .UseIdentityAlwaysColumn(),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    var column = Assert.Single(table.Columns);
                    Assert.Equal(NpgsqlValueGenerationStrategy.IdentityAlwaysColumn, column[NpgsqlAnnotationNames.ValueGenerationStrategy]);
                    Assert.Null(column[NpgsqlAnnotationNames.IdentityOptions]);
                });

            AssertSql(
                @"CREATE TABLE ""People"" (
    ""Id"" integer GENERATED ALWAYS AS IDENTITY
);");
        }

        [Fact]
        public virtual async Task Create_table_with_identity_always_with_options()
        {
            await Test(
                builder => { },
                builder => builder.Entity("People").Property<int>("Id")
                    .UseIdentityAlwaysColumn()
                    .HasIdentityOptions(startValue: 10, incrementBy: 2, maxValue: 2000),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    var column = Assert.Single(table.Columns);
                    Assert.Equal(NpgsqlValueGenerationStrategy.IdentityAlwaysColumn, column[NpgsqlAnnotationNames.ValueGenerationStrategy]);
                    var options = IdentitySequenceOptionsData.Get(column);
                    Assert.Equal(10, options.StartValue);
                    Assert.Equal(2, options.IncrementBy);
                    Assert.Equal(2000, options.MaxValue);
                    Assert.Null(options.MinValue);
                    Assert.Equal(1, options.NumbersToCache);
                    Assert.False(options.IsCyclic);
                });

            AssertSql(
                @"CREATE TABLE ""People"" (
    ""Id"" integer GENERATED ALWAYS AS IDENTITY (START WITH 10 INCREMENT BY 2 MAXVALUE 2000)
);");
        }

        [Fact]
        public virtual async Task Create_table_with_serial()
        {
            await Test(
                builder => { },
                builder => builder.Entity("People").Property<int>("Id")
                    .UseSerialColumn(),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    var column = Assert.Single(table.Columns);
                    Assert.Equal(NpgsqlValueGenerationStrategy.SerialColumn, column[NpgsqlAnnotationNames.ValueGenerationStrategy]);

                    Assert.Empty(model.Sequences);
                });

            AssertSql(
                @"CREATE TABLE ""People"" (
    ""Id"" serial NOT NULL
);");
        }

        [Fact]
        public virtual async Task Create_table_with_system_column()
        {
            // System columns (e.g. xmin) are implicitly always present. If an xmin property is present,
            // nothing should happen.
            await Test(
                builder => { },
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.Property<uint>("xmin");
                        e.HasKey("Id");
                    }),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    Assert.Equal("Id", Assert.Single(table.Columns).Name);
                });

            AssertSql(
                @"CREATE TABLE ""People"" (
    ""Id"" integer NOT NULL,
    CONSTRAINT ""PK_People"" PRIMARY KEY (""Id"")
);");
        }

        [Fact]
        public virtual async Task Create_table_with_storage_parameter()
        {
            await Test(
                builder => { },
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.HasKey("Id");
                        e.HasStorageParameter("fillfactor", 70);
                        e.HasStorageParameter("user_catalog_table", true);
                    }),
                asserter: null);  // We don't scaffold storage parameters

            AssertSql(
                @"CREATE TABLE ""People"" (
    ""Id"" integer NOT NULL,
    CONSTRAINT ""PK_People"" PRIMARY KEY (""Id"")
)
WITH (fillfactor=70, user_catalog_table=true);");
        }

        [Fact]
        public virtual async Task Create_table_with_unlogged()
        {
            await Test(
                builder => { },
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.HasKey("Id");
                        e.IsUnlogged();
                    }),
                asserter: null);  // We don't scaffold unlogged

            AssertSql(
                @"CREATE UNLOGGED TABLE ""People"" (
    ""Id"" integer NOT NULL,
    CONSTRAINT ""PK_People"" PRIMARY KEY (""Id"")
);");
        }

        [Fact]
        public virtual async Task Create_table_with_list_partitioning()
        {
            await Test(
                builder => { },
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.HasKey("Id");
                        e.IsPartitioned(TablePartitioningType.List, "Id");
                    }),
                asserter: null);

            AssertSql(
                @"CREATE TABLE ""People"" (
    ""Id"" integer NOT NULL,
    CONSTRAINT ""PK_People"" PRIMARY KEY (""Id"")
)
PARTITION BY List (""Id"") ;");
        }

        [Fact]
        public virtual async Task Create_table_with_range_partitioning()
        {
            await Test(
                builder => { },
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.HasKey("Id");
                        e.IsPartitioned(TablePartitioningType.Range, "Id");
                    }),
                asserter: null);

            AssertSql(
                @"CREATE TABLE ""People"" (
    ""Id"" integer NOT NULL,
    CONSTRAINT ""PK_People"" PRIMARY KEY (""Id"")
)
PARTITION BY Range (""Id"") ;");
        }

        [Fact]
        public virtual async Task Create_table_with_hash_partitioning()
        {
            await Test(
                builder => { },
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.HasKey("Id");
                        e.IsPartitioned(TablePartitioningType.Hash, "Id");
                    }),
                asserter: null);

            AssertSql(
                @"CREATE TABLE ""People"" (
    ""Id"" integer NOT NULL,
    CONSTRAINT ""PK_People"" PRIMARY KEY (""Id"")
)
PARTITION BY Hash (""Id"") ;");
        }

        [Fact]
        public virtual async Task Create_table_with_multiple_partitioning_columns()
        {
            await Test(
                builder => { },
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.Property<int>("Location");
                        e.HasKey("Id", "Location");
                        e.IsPartitioned(TablePartitioningType.Hash, "Id", "Location");
                    }),
                asserter: null);

            AssertSql(
                @"CREATE TABLE ""People"" (
    ""Id"" integer NOT NULL,
    ""Location"" integer NOT NULL,
    CONSTRAINT ""PK_People"" PRIMARY KEY (""Id"", ""Location"")
)
PARTITION BY Hash (""Id"", ""Location"") ;");
        }

        [Fact]
        public virtual async Task Create_table_with_partitioning_with_custom_column_name()
        {
            await Test(
                builder => { },
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.Property<int>("Location").HasColumnName("loc");
                        e.HasKey("Id", "Location");
                        e.IsPartitioned(TablePartitioningType.Hash, "Id", "Location");
                    }),
                asserter: null);

            AssertSql(
                @"CREATE TABLE ""People"" (
    ""Id"" integer NOT NULL,
    cat integer NOT NULL,
    CONSTRAINT ""PK_People"" PRIMARY KEY (""Id"", loc)
)
PARTITION BY Hash (""Id"", loc) ;");
        }

        public override async Task Drop_table()
        {
            await base.Drop_table();

            AssertSql(
                @"DROP TABLE ""People"";");
        }

        public override async Task Alter_table_add_comment()
        {
            await base.Alter_table_add_comment();

            AssertSql(
                @"COMMENT ON TABLE ""People"" IS 'Table comment';");
        }

        public override async Task Alter_table_add_comment_non_default_schema()
        {
            await base.Alter_table_add_comment_non_default_schema();

            AssertSql(
                @"COMMENT ON TABLE ""SomeOtherSchema"".""People"" IS 'Table comment';");
        }

        public override async Task Alter_table_change_comment()
        {
            await base.Alter_table_change_comment();

            AssertSql(
                @"COMMENT ON TABLE ""People"" IS 'Table comment2';");
        }

        public override async Task Alter_table_remove_comment()
        {
            await base.Alter_table_remove_comment();

            AssertSql(
                @"COMMENT ON TABLE ""People"" IS NULL;");
        }

        [Fact]
        public virtual async Task Alter_table_change_storage_parameters()
        {
            await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.HasKey("Id");
                    }),
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.HasStorageParameter("fillfactor", 70);
                        e.HasStorageParameter("user_catalog_table", true);
                        e.HasStorageParameter("parallel_workers", 8);
                    }),
                builder => builder.Entity(
                    "People", e =>
                    {
                        // Add parameter
                        e.HasStorageParameter("autovacuum_enabled", true);
                        // Change parameter
                        e.HasStorageParameter("fillfactor", 80);
                        // Drop parameter user_catalog
                        // Leave parameter unchanged
                        e.HasStorageParameter("parallel_workers", 8);
                    }),
                asserter: null);  // We don't scaffold storage parameters

            AssertSql(
                @"ALTER TABLE ""People"" SET (autovacuum_enabled=true, fillfactor=80);
ALTER TABLE ""People"" RESET (user_catalog_table);");
        }

        [Fact]
        public virtual async Task Alter_table_make_unlogged()
        {
            await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.HasKey("Id");
                    }),
                builder => { },
                builder => builder.Entity("People").IsUnlogged(),
                asserter: null);  // We don't scaffold unlogged

            AssertSql(
                @"ALTER TABLE ""People"" SET UNLOGGED;");
        }

        [Fact]
        public virtual async Task Alter_table_add_partitioning_throws_exception()
        {
            await Assert.ThrowsAsync<ArgumentException>(async () => await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.HasKey("Id");
                    }),
                builder => { },
                builder => builder.Entity("People", e => e.IsPartitioned(TablePartitioningType.List, "Id")),
                asserter: null));
        }

        [Fact]
        public virtual async Task Alter_table_change_partitioning_type_throws_exception()
        {
            await Assert.ThrowsAsync<ArgumentException>(async () => await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.HasKey("Id");
                        e.IsPartitioned(TablePartitioningType.List, "Id");
                    }),
                builder => { },
                builder => builder.Entity("People", e => e.IsPartitioned(TablePartitioningType.Range, "Id")),
                asserter: null));
        }

        [Fact]
        public virtual async Task Alter_table_change_partitioning_key_throws_exception()
        {
            await Assert.ThrowsAsync<ArgumentException>(async () => await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.Property<int>("Location");
                        e.HasKey("Id", "Location");
                        e.IsPartitioned(TablePartitioningType.List, "Id");
                    }),
                builder => { },
                builder => builder.Entity("People", e => e.IsPartitioned(TablePartitioningType.List, "Id", "Location")),
                asserter: null));
        }

        [Fact]
        public virtual async Task Alter_table_make_logged()
        {
            await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.HasKey("Id");
                    }),
                builder => builder.Entity("People").IsUnlogged(),
                builder => { },
                asserter: null);  // We don't scaffold unlogged

            AssertSql(
                @"ALTER TABLE ""People"" SET LOGGED;");
        }

        public override async Task Rename_table()
        {
            await base.Rename_table();

            AssertSql(
                @"ALTER TABLE ""People"" RENAME TO ""Persons"";");
        }

        public override async Task Rename_table_with_primary_key()
        {
            await base.Rename_table_with_primary_key();

            AssertSql(
                @"ALTER TABLE ""People"" DROP CONSTRAINT ""PK_People"";",
                //
                @"ALTER TABLE ""People"" RENAME TO ""Persons"";",
                //
                @"ALTER TABLE ""Persons"" ADD CONSTRAINT ""PK_Persons"" PRIMARY KEY (""Id"");");
        }

        public override async Task Move_table()
        {
            await base.Move_table();

            AssertSql(
                @"CREATE SCHEMA IF NOT EXISTS ""TestTableSchema"";",
                //
                @"ALTER TABLE ""TestTable"" SET SCHEMA ""TestTableSchema"";");
        }

        #endregion

        #region Schema

        public override async Task Create_schema()
        {
            await base.Create_schema();

            AssertSql(
                @"CREATE SCHEMA IF NOT EXISTS ""SomeOtherSchema"";",
                //
                @"CREATE TABLE ""SomeOtherSchema"".""People"" (
    ""Id"" integer NOT NULL
);");
        }

        [Fact]
        public virtual async Task Create_schema_public_is_ignored()
        {
            await Test(
                builder => { },
                builder => builder.Entity("People")
                    .ToTable("People", "public")
                    .Property<int>("Id"),
                model => Assert.Equal("public", Assert.Single(model.Tables).Schema));

            AssertSql(
                @"CREATE TABLE public.""People"" (
    ""Id"" integer NOT NULL
);");
        }

        #endregion

        #region Column

        public override async Task Add_column_with_defaultValue_string()
        {
            await base.Add_column_with_defaultValue_string();

            AssertSql(
                @"ALTER TABLE ""People"" ADD ""Name"" text NOT NULL DEFAULT 'John Doe';");
        }

        public override async Task Add_column_with_defaultValue_datetime()
        {
            await base.Add_column_with_defaultValue_datetime();

            AssertSql(
                @"ALTER TABLE ""People"" ADD ""Birthday"" timestamp without time zone NOT NULL DEFAULT TIMESTAMP '2015-04-12 17:05:00';");
        }

        [Fact]
        public override async Task Add_column_with_defaultValueSql()
        {
            await base.Add_column_with_defaultValueSql();

            AssertSql(
                @"ALTER TABLE ""People"" ADD ""Sum"" integer NOT NULL DEFAULT (1 + 2);");
        }

        public override async Task Add_column_with_computedSql(bool? stored)
        {
            if (TestEnvironment.PostgresVersion.IsUnder(12))
            {
                await Assert.ThrowsAsync<NotSupportedException>(() => base.Add_column_with_computedSql(stored));
                return;
            }

            if (stored != true)
            {
                // Non-stored generated columns aren't yet supported (PG12)
                await Assert.ThrowsAsync<NotSupportedException>(() => base.Add_column_with_computedSql(stored));
                return;
            }

            await base.Add_column_with_computedSql(stored: true);

            AssertSql(
                @"ALTER TABLE ""People"" ADD ""Sum"" text GENERATED ALWAYS AS (""X"" + ""Y"") STORED;");
        }

        public override async Task Add_column_with_required()
        {
            await base.Add_column_with_required();

            AssertSql(
                @"ALTER TABLE ""People"" ADD ""Name"" text NOT NULL DEFAULT '';");
        }

        public override async Task Add_column_with_ansi()
        {
            await base.Add_column_with_ansi();

            AssertSql(
                @"ALTER TABLE ""People"" ADD ""Name"" text NULL;");
        }

        public override async Task Add_column_with_max_length()
        {
            await base.Add_column_with_max_length();

            AssertSql(
                @"ALTER TABLE ""People"" ADD ""Name"" character varying(30) NULL;");
        }

        public override async Task Add_column_with_max_length_on_derived()
        {
            await base.Add_column_with_max_length_on_derived();

            AssertSql();
        }

        public override async Task Add_column_with_fixed_length()
        {
            await base.Add_column_with_fixed_length();

            AssertSql(
                @"ALTER TABLE ""People"" ADD ""Name"" character(100) NULL;");
        }

        public override async Task Add_column_with_comment()
        {
            await base.Add_column_with_comment();

            AssertSql(
                @"ALTER TABLE ""People"" ADD ""FullName"" text NULL;
COMMENT ON COLUMN ""People"".""FullName"" IS 'My comment';");
        }

        [ConditionalFact]
        public override async Task Add_column_with_collation()
        {
            await base.Add_column_with_collation();

            AssertSql(
                @"ALTER TABLE ""People"" ADD ""Name"" text COLLATE ""POSIX"" NULL;");
        }

        [ConditionalFact]
        public override async Task Add_column_computed_with_collation()
        {
            if (TestEnvironment.PostgresVersion.IsUnder(12))
            {
                await Assert.ThrowsAsync<NotSupportedException>(() => base.Add_column_computed_with_collation());
                return;
            }

            // Non-stored generated columns aren't yet supported (PG12), so we override to used stored
            await Test(
                builder => builder.Entity("People").Property<int>("Id"),
                builder => { },
                builder => builder.Entity("People").Property<string>("Name")
                    .HasComputedColumnSql("'hello'", stored: true)
                    .UseCollation(NonDefaultCollation),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    Assert.Equal(2, table.Columns.Count);
                    var nameColumn = Assert.Single(table.Columns, c => c.Name == "Name");
                    Assert.Contains("hello", nameColumn.ComputedColumnSql);
                    Assert.Equal(NonDefaultCollation, nameColumn.Collation);
                });

            AssertSql(
                @"ALTER TABLE ""People"" ADD ""Name"" text COLLATE ""POSIX"" GENERATED ALWAYS AS ('hello') STORED;");
        }

        [ConditionalFact]
        public async Task Add_column_with_default_column_collation()
        {
            await Test(
                builder =>
                {
                    builder.UseDefaultColumnCollation("POSIX");
                    builder.Entity("People").Property<int>("Id");
                },
                builder => { },
                builder => builder.Entity("People").Property<string>("Name"),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    Assert.Equal(2, table.Columns.Count);
                    var nameColumn = Assert.Single(table.Columns, c => c.Name == "Name");
                    Assert.Equal("POSIX", nameColumn.Collation);
                });

            AssertSql(
                @"ALTER TABLE ""People"" ADD ""Name"" text COLLATE ""POSIX"" NULL;");
        }

        [ConditionalFact]
        public async Task Add_column_with_collation_overriding_default()
        {
            await Test(
                builder =>
                {
                    builder.UseDefaultColumnCollation("POSIX");
                    builder.Entity("People").Property<int>("Id");
                },
                builder => { },
                builder => builder.Entity("People").Property<string>("Name")
                    .UseCollation("C"),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    Assert.Equal(2, table.Columns.Count);
                    var nameColumn = Assert.Single(table.Columns, c => c.Name == "Name");
                    Assert.Equal("C", nameColumn.Collation);
                });

            AssertSql(
                @"ALTER TABLE ""People"" ADD ""Name"" text COLLATE ""C"" NULL;");
        }

        public override async Task Add_column_shared()
        {
            await base.Add_column_shared();

            AssertSql();
        }

        [Fact]
        public virtual async Task Add_column_with_upper_case_store_type()
        {
            // At least for now, it's the user's responsibility to quote store type name when needed,
            // because it seems standard for people to specify either text or TEXT, and both should work.
            await Test(
                builder => { },
                builder => builder.Entity("People").Property<string>("Name").HasColumnType("TEXT"),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    var column = Assert.Single(table.Columns, c => c.Name == "Name");
                    Assert.Equal("text", column.StoreType);
                });

            AssertSql(
                @"CREATE TABLE ""People"" (
    ""Name"" TEXT NULL
);");
        }

        [Fact]
        public virtual async Task Add_column_with_identity_by_default()
        {
            await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.HasKey("Id");
                    }),
                builder => { },
                builder => builder.Entity("People").Property<int?>("SomeColumn")
                    .UseIdentityByDefaultColumn(),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    var column = Assert.Single(table.Columns, c => c.Name == "SomeColumn");
                    Assert.Equal(NpgsqlValueGenerationStrategy.IdentityByDefaultColumn, column[NpgsqlAnnotationNames.ValueGenerationStrategy]);
                    Assert.Null(column[NpgsqlAnnotationNames.IdentityOptions]);
                });

            AssertSql(
                @"ALTER TABLE ""People"" ADD ""SomeColumn"" integer GENERATED BY DEFAULT AS IDENTITY;");
        }

        [Fact]
        public virtual async Task Add_column_with_identity_always()
        {
            await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.HasKey("Id");
                    }),
                builder => { },
                builder => builder.Entity("People").Property<int?>("SomeColumn")
                    .UseIdentityAlwaysColumn(),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    var column = Assert.Single(table.Columns, c => c.Name == "SomeColumn");
                    Assert.Equal(NpgsqlValueGenerationStrategy.IdentityAlwaysColumn, column[NpgsqlAnnotationNames.ValueGenerationStrategy]);
                    Assert.Null(column[NpgsqlAnnotationNames.IdentityOptions]);
                });

            AssertSql(
                @"ALTER TABLE ""People"" ADD ""SomeColumn"" integer GENERATED ALWAYS AS IDENTITY;");
        }

        [Fact]
        public virtual async Task Add_column_with_identity_by_default_with_all_options()
        {
            await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.HasKey("Id");
                    }),
                builder => { },
                builder => builder.Entity("People").Property<int?>("SomeColumn")
                    .UseIdentityByDefaultColumn()
                    .HasIdentityOptions(
                        startValue: 5,
                        incrementBy: 2,
                        minValue: 3,
                        maxValue: 2000,
                        cyclic: true,
                        numbersToCache: 10),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    var column = Assert.Single(table.Columns, c => c.Name == "SomeColumn");
                    Assert.Equal(NpgsqlValueGenerationStrategy.IdentityByDefaultColumn, column[NpgsqlAnnotationNames.ValueGenerationStrategy]);
                    var options = IdentitySequenceOptionsData.Get(column);
                    Assert.Equal(5, options.StartValue);
                    Assert.Equal(2, options.IncrementBy);
                    Assert.Equal(3, options.MinValue);
                    Assert.Equal(2000, options.MaxValue);
                    Assert.True(options.IsCyclic);
                    Assert.Equal(10, options.NumbersToCache);
                });

            AssertSql(
                @"ALTER TABLE ""People"" ADD ""SomeColumn"" integer GENERATED BY DEFAULT AS IDENTITY (START WITH 5 INCREMENT BY 2 MINVALUE 3 MAXVALUE 2000 CYCLE CACHE 10);");
        }

        [Fact]
        public virtual Task Add_column_optional_with_serial_not_supported()
            => TestThrows<NotSupportedException>(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.HasKey("Id");
                    }),
                builder => { },
                builder => builder.Entity("People").Property<int?>("SomeColumn")
                    .UseSerialColumn());

        [Fact]
        public virtual async Task Add_column_required_with_serial()
        {
            await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.HasKey("Id");
                    }),
                builder => { },
                builder => builder.Entity("People").Property<int>("SomeColumn")
                    .UseSerialColumn(),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    var column = Assert.Single(table.Columns, c => c.Name == "SomeColumn");
                    Assert.Equal(NpgsqlValueGenerationStrategy.SerialColumn, column[NpgsqlAnnotationNames.ValueGenerationStrategy]);
                    Assert.Null(column[NpgsqlAnnotationNames.IdentityOptions]);
                });

            AssertSql(
                @"ALTER TABLE ""People"" ADD ""SomeColumn"" serial NOT NULL;");
        }

        [Fact]
        public virtual async Task Add_column_required_with_identity_by_default()
        {
            await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.HasKey("Id");
                    }),
                builder => { },
                builder => builder.Entity("People").Property<int>("SomeColumn")
                    .UseIdentityByDefaultColumn(),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    var column = Assert.Single(table.Columns, c => c.Name == "SomeColumn");
                    Assert.Equal(NpgsqlValueGenerationStrategy.IdentityByDefaultColumn, column[NpgsqlAnnotationNames.ValueGenerationStrategy]);
                    Assert.Null(column[NpgsqlAnnotationNames.IdentityOptions]);
                });

            AssertSql(
                @"ALTER TABLE ""People"" ADD ""SomeColumn"" integer GENERATED BY DEFAULT AS IDENTITY;");
        }

        [Fact]
        public virtual async Task Add_column_system()
        {
            // System columns (e.g. xmin) are implicitly always present. If an xmin property is added,
            // nothing should happen.
            await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.HasKey("Id");
                    }),
                builder => { },
                builder => builder.Entity("People").Property<uint>("xmin"),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    Assert.Equal("Id", Assert.Single(table.Columns).Name);
                });

            AssertSql();
        }

        [Fact]
        public virtual async Task Add_column_with_huge_varchar()
        {
             // PostgreSQL doesn't allow varchar(x) with x > 10485760, so we map this to text.
             // See #342 and https://www.postgresql.org/message-id/15790.1291824247%40sss.pgh.pa.us
            await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.HasKey("Id");
                    }),
                builder => { },
                builder => builder.Entity("People").Property<string>("Name").HasMaxLength(10485761),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    var column = Assert.Single(table.Columns, c => c.Name == "Name");
                    Assert.Equal("text", column.StoreType);
                });

            AssertSql(
                @"ALTER TABLE ""People"" ADD ""Name"" text NULL;");
        }

        [Fact]
        public virtual async Task Add_column_generated_tsvector()
        {
            if (TestEnvironment.PostgresVersion.IsUnder(12))
                return;

            await Test(
                builder => builder.Entity(
                    "Blogs", e =>
                    {
                        e.Property<string>("Title").IsRequired();
                        e.Property<string>("Description");
                    }),
                builder => { },
                builder => builder.Entity("Blogs").Property<NpgsqlTsVector>("TsVector")
                    .IsGeneratedTsVectorColumn("english", "Title", "Description"),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    var column = Assert.Single(table.Columns, c => c.Name == "TsVector");
                    Assert.Equal("tsvector", column.StoreType);
                    Assert.Equal(@"to_tsvector('english'::regconfig, ((""Title"" || ' '::text) || COALESCE(""Description"", ''::text)))", column.ComputedColumnSql);
                });

            AssertSql(
                @"ALTER TABLE ""Blogs"" ADD ""TsVector"" tsvector GENERATED ALWAYS AS (to_tsvector('english', ""Title"" || ' ' || coalesce(""Description"", ''))) STORED;");
        }

        public override async Task Alter_column_change_type()
        {
            await base.Alter_column_change_type();

            AssertSql(
                @"ALTER TABLE ""People"" ALTER COLUMN ""SomeColumn"" TYPE bigint;");
        }

        public override async Task Alter_column_make_required()
        {
            await base.Alter_column_make_required();

            AssertSql(
                @"ALTER TABLE ""People"" ALTER COLUMN ""SomeColumn"" SET NOT NULL;
ALTER TABLE ""People"" ALTER COLUMN ""SomeColumn"" SET DEFAULT '';");
        }

        public override async Task Alter_column_make_required_with_index()
        {
            await base.Alter_column_make_required_with_index();

            AssertSql(
                @"ALTER TABLE ""People"" ALTER COLUMN ""SomeColumn"" SET NOT NULL;
ALTER TABLE ""People"" ALTER COLUMN ""SomeColumn"" SET DEFAULT '';");
        }

        public override async Task Alter_column_make_required_with_composite_index()
        {
            await base.Alter_column_make_required_with_composite_index();

            AssertSql(
                @"ALTER TABLE ""People"" ALTER COLUMN ""FirstName"" SET NOT NULL;
ALTER TABLE ""People"" ALTER COLUMN ""FirstName"" SET DEFAULT '';");
        }

        public override async Task Alter_column_make_computed(bool? stored)
        {
            if (TestEnvironment.PostgresVersion.IsUnder(12))
            {
                await Assert.ThrowsAsync<NotSupportedException>(() => base.Add_column_with_computedSql(stored));
                return;
            }

            if (stored != true)
            {
                // Non-stored generated columns aren't yet supported (PG12)
                await Assert.ThrowsAsync<NotSupportedException>(() => base.Add_column_with_computedSql(stored));
                return;
            }

            await base.Alter_column_make_computed(stored);

            AssertSql(
                @"ALTER TABLE ""People"" DROP COLUMN ""Sum"";",
                //
                @"ALTER TABLE ""People"" ADD ""Sum"" integer GENERATED ALWAYS AS (""X"" + ""Y"") STORED;");
        }

        public override async Task Alter_column_change_computed()
        {
            if (TestEnvironment.PostgresVersion.IsUnder(12))
            {
                await Assert.ThrowsAsync<NotSupportedException>(() => base.Alter_column_change_computed());
                return;
            }

            // Non-stored generated columns aren't yet supported (PG12), so we override to used stored
            await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.Property<int>("X");
                        e.Property<int>("Y");
                        e.Property<int>("Sum");
                    }),
                builder => builder.Entity("People").Property<int>("Sum")
                    .HasComputedColumnSql($"{DelimitIdentifier("X")} + {DelimitIdentifier("Y")}", stored: true),
                builder => builder.Entity("People").Property<int>("Sum")
                    .HasComputedColumnSql($"{DelimitIdentifier("X")} - {DelimitIdentifier("Y")}", stored: true),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    var sumColumn = Assert.Single(table.Columns, c => c.Name == "Sum");
                    Assert.Contains("X", sumColumn.ComputedColumnSql);
                    Assert.Contains("Y", sumColumn.ComputedColumnSql);
                    Assert.Contains("-", sumColumn.ComputedColumnSql);
                });

            AssertSql(
                @"ALTER TABLE ""People"" DROP COLUMN ""Sum"";",
                //
                @"ALTER TABLE ""People"" ADD ""Sum"" integer GENERATED ALWAYS AS (""X"" - ""Y"") STORED;");
        }

        public override Task Alter_column_change_computed_type()
            => Assert.ThrowsAsync<NotSupportedException>(() => base.Alter_column_change_computed());

        public override async Task Alter_column_add_comment()
        {
            await base.Alter_column_add_comment();

            AssertSql(
                @"COMMENT ON COLUMN ""People"".""Id"" IS 'Some comment';");
        }

        public override async Task Alter_column_change_comment()
        {
            await base.Alter_column_change_comment();

            AssertSql(
                @"COMMENT ON COLUMN ""People"".""Id"" IS 'Some comment2';");
        }

        public override async Task Alter_column_remove_comment()
        {
            await base.Alter_column_remove_comment();

            AssertSql(
                @"COMMENT ON COLUMN ""People"".""Id"" IS NULL;");
        }

        [Fact]
        public virtual async Task Alter_column_make_identity_by_default()
        {
            await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.HasKey("Id");
                    }),
                builder => { },
                builder => builder.Entity("People").Property<int>("Id")
                    .UseIdentityByDefaultColumn(),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    var column = Assert.Single(table.Columns);
                    Assert.Equal(NpgsqlValueGenerationStrategy.IdentityByDefaultColumn, column[NpgsqlAnnotationNames.ValueGenerationStrategy]);
                    Assert.Null(column[NpgsqlAnnotationNames.IdentityOptions]);
                });

            AssertSql(
                @"ALTER TABLE ""People"" ALTER COLUMN ""Id"" DROP DEFAULT;
ALTER TABLE ""People"" ALTER COLUMN ""Id"" ADD GENERATED BY DEFAULT AS IDENTITY;");
        }

        [Fact]
        public virtual async Task Alter_column_make_identity_always()
        {
            await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.HasKey("Id");
                    }),
                builder => { },
                builder => builder.Entity("People").Property<int>("Id")
                    .UseIdentityAlwaysColumn(),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    var column = Assert.Single(table.Columns);
                    Assert.Equal(NpgsqlValueGenerationStrategy.IdentityAlwaysColumn, column[NpgsqlAnnotationNames.ValueGenerationStrategy]);
                    Assert.Null(column[NpgsqlAnnotationNames.IdentityOptions]);
                });

            AssertSql(
                @"ALTER TABLE ""People"" ALTER COLUMN ""Id"" DROP DEFAULT;
ALTER TABLE ""People"" ALTER COLUMN ""Id"" ADD GENERATED ALWAYS AS IDENTITY;");
        }

        [Fact]
        public virtual async Task Alter_column_make_default_into_identity()
        {
            await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id").HasDefaultValue(8);
                        e.HasKey("Id");
                    }),
                builder => { },
                builder => builder.Entity("People").Property<int>("Id")
                    .UseIdentityAlwaysColumn(),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    var column = Assert.Single(table.Columns);
                    Assert.Equal(NpgsqlValueGenerationStrategy.IdentityAlwaysColumn, column[NpgsqlAnnotationNames.ValueGenerationStrategy]);
                    Assert.Null(column[NpgsqlAnnotationNames.IdentityOptions]);
                });

            AssertSql(
                @"ALTER TABLE ""People"" ALTER COLUMN ""Id"" DROP DEFAULT;
ALTER TABLE ""People"" ALTER COLUMN ""Id"" ADD GENERATED ALWAYS AS IDENTITY;");
        }

        [Fact]
        public virtual async Task Alter_column_make_identity_by_default_with_options()
        {
            await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.HasKey("Id");
                    }),
                builder => { },
                builder => builder.Entity("People").Property<int>("Id")
                    .UseIdentityByDefaultColumn()
                    .HasIdentityOptions(startValue: 10, incrementBy: 2, maxValue: 2000),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    var column = Assert.Single(table.Columns);
                    Assert.Equal(NpgsqlValueGenerationStrategy.IdentityByDefaultColumn, column[NpgsqlAnnotationNames.ValueGenerationStrategy]);
                    var options = IdentitySequenceOptionsData.Get(column);
                    Assert.Equal(10, options.StartValue);
                    Assert.Equal(2, options.IncrementBy);
                    Assert.Equal(2000, options.MaxValue);
                    Assert.Null(options.MinValue);
                    Assert.Equal(1, options.NumbersToCache);
                    Assert.False(options.IsCyclic);
                });

            AssertSql(
                @"ALTER TABLE ""People"" ALTER COLUMN ""Id"" DROP DEFAULT;
ALTER TABLE ""People"" ALTER COLUMN ""Id"" ADD GENERATED BY DEFAULT AS IDENTITY (START WITH 10 INCREMENT BY 2 MAXVALUE 2000);");
        }

        [Fact]
        public virtual async Task Alter_column_make_identity_with_default_options()
        {
            await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.HasKey("Id");
                    }),
                builder => { },
                builder => builder.Entity("People").Property<int>("Id")
                    .UseIdentityByDefaultColumn()
                    .HasIdentityOptions(startValue: 1, incrementBy: 1, minValue: 1, maxValue: null),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    var column = Assert.Single(table.Columns);
                    Assert.Equal(NpgsqlValueGenerationStrategy.IdentityByDefaultColumn, column[NpgsqlAnnotationNames.ValueGenerationStrategy]);
                    var options = IdentitySequenceOptionsData.Get(column);
                    Assert.Null(options.StartValue);
                    Assert.Equal(1, options.IncrementBy);
                    Assert.Null(options.MaxValue);
                    Assert.Null(options.MinValue);
                    Assert.Equal(1, options.NumbersToCache);
                    Assert.False(options.IsCyclic);
                });

            AssertSql(
                @"ALTER TABLE ""People"" ALTER COLUMN ""Id"" DROP DEFAULT;
ALTER TABLE ""People"" ALTER COLUMN ""Id"" ADD GENERATED BY DEFAULT AS IDENTITY;");
        }

        [Fact]
        public virtual async Task Alter_column_change_identity_options()
        {
            await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id").UseIdentityByDefaultColumn();
                        e.HasKey("Id");
                    }),
                builder => builder.Entity("People").Property<int>("Id")
                    .HasIdentityOptions(incrementBy: 1, maxValue: 1000, cyclic: false),
                builder => builder.Entity("People").Property<int>("Id")
                    .HasIdentityOptions(incrementBy: 2, maxValue: 1000, cyclic: true),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    var column = Assert.Single(table.Columns);
                    Assert.Equal(NpgsqlValueGenerationStrategy.IdentityByDefaultColumn, column[NpgsqlAnnotationNames.ValueGenerationStrategy]);
                    var options = IdentitySequenceOptionsData.Get(column);
                    Assert.Equal(2, options.IncrementBy);
                    Assert.Equal(1000, options.MaxValue);
                    Assert.True(options.IsCyclic);
                });

            AssertSql(
                @"ALTER TABLE ""People"" ALTER COLUMN ""Id"" SET INCREMENT BY 2;
ALTER TABLE ""People"" ALTER COLUMN ""Id"" SET CYCLE;");
        }

        [Fact]
        public virtual async Task Alter_column_remove_identity_options()
        {
            await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id").UseIdentityByDefaultColumn();
                        e.HasKey("Id");
                    }),
                builder => builder.Entity("People").Property<int>("Id")
                    .HasIdentityOptions(startValue: 5, incrementBy: 2, cyclic: true, numbersToCache: 5),
                builder => { },
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    var column = Assert.Single(table.Columns);
                    Assert.Equal(NpgsqlValueGenerationStrategy.IdentityByDefaultColumn, column[NpgsqlAnnotationNames.ValueGenerationStrategy]);
                    var options = IdentitySequenceOptionsData.Get(column);
                    Assert.Equal(5, options.StartValue);  // Restarting doesn't change the scaffolded start value
                    Assert.Equal(1, options.IncrementBy);
                    Assert.False(options.IsCyclic);
                    Assert.Equal(1, options.NumbersToCache);
                });

            AssertSql(
                @"ALTER TABLE ""People"" ALTER COLUMN ""Id"" RESTART WITH 1;
ALTER TABLE ""People"" ALTER COLUMN ""Id"" SET INCREMENT BY 1;
ALTER TABLE ""People"" ALTER COLUMN ""Id"" SET NO CYCLE;
ALTER TABLE ""People"" ALTER COLUMN ""Id"" SET CACHE 1;");
        }

        [Fact]
        public virtual async Task Alter_column_make_serial()
        {
            await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.HasKey("Id");
                    }),
                builder => { },
                builder => builder.Entity("People").Property<int>("Id")
                    .UseSerialColumn(),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    var column = Assert.Single(table.Columns);
                    Assert.Equal(NpgsqlValueGenerationStrategy.SerialColumn, column[NpgsqlAnnotationNames.ValueGenerationStrategy]);
                    Assert.Null(column[NpgsqlAnnotationNames.IdentityOptions]);

                    Assert.Empty(model.Sequences);
                });

            AssertSql(
                @"CREATE SEQUENCE ""People_Id_seq"" AS integer START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE NO CYCLE;",
                //
                @"ALTER TABLE ""People"" ALTER COLUMN ""Id"" SET DEFAULT (nextval('""People_Id_seq""'));
ALTER SEQUENCE ""People_Id_seq"" OWNED BY ""People"".""Id"";");
        }

        [Fact]
        public virtual async Task Alter_column_make_serial_in_non_default_schema()
        {
            await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.ToTable("People", "some_schema");
                        e.Property<int>("Id");
                        e.HasKey("Id");
                    }),
                builder => { },
                builder => builder.Entity("People").Property<int>("Id")
                    .UseSerialColumn(),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    var column = Assert.Single(table.Columns);
                    Assert.Equal(NpgsqlValueGenerationStrategy.SerialColumn, column[NpgsqlAnnotationNames.ValueGenerationStrategy]);
                    Assert.Null(column[NpgsqlAnnotationNames.IdentityOptions]);

                    Assert.Empty(model.Sequences);
                });

            AssertSql(
                @"CREATE SEQUENCE some_schema.""People_Id_seq"" AS integer START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE NO CYCLE;",
                //
                @"ALTER TABLE some_schema.""People"" ALTER COLUMN ""Id"" SET DEFAULT (nextval('some_schema.""People_Id_seq""'));
ALTER SEQUENCE some_schema.""People_Id_seq"" OWNED BY some_schema.""People"".""Id"";");
        }

        [Fact]
        public virtual async Task Alter_column_long_make_bigserial()
        {
            await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<long>("Id");
                        e.HasKey("Id");
                    }),
                builder => { },
                builder => builder.Entity("People").Property<long>("Id")
                    .UseSerialColumn(),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    var column = Assert.Single(table.Columns);
                    Assert.Equal(NpgsqlValueGenerationStrategy.SerialColumn, column[NpgsqlAnnotationNames.ValueGenerationStrategy]);
                    Assert.Equal("bigint", column.StoreType);
                    Assert.Null(column[NpgsqlAnnotationNames.IdentityOptions]);

                    Assert.Empty(model.Sequences);
                });

            AssertSql(
                @"CREATE SEQUENCE ""People_Id_seq"" START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE NO CYCLE;",
                //
                @"ALTER TABLE ""People"" ALTER COLUMN ""Id"" SET DEFAULT (nextval('""People_Id_seq""'));
ALTER SEQUENCE ""People_Id_seq"" OWNED BY ""People"".""Id"";");
        }

        [Fact]
        public virtual async Task Alter_column_change_identity_type()
        {
            await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.HasKey("Id");
                    }),
                builder => builder.Entity("People").Property<int>("Id").UseIdentityByDefaultColumn(),
                builder => builder.Entity("People").Property<int>("Id").UseIdentityAlwaysColumn(),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    var column = Assert.Single(table.Columns);
                    Assert.Equal(NpgsqlValueGenerationStrategy.IdentityAlwaysColumn, column[NpgsqlAnnotationNames.ValueGenerationStrategy]);
                    Assert.Null(column[NpgsqlAnnotationNames.IdentityOptions]);
                });

            AssertSql(
                @"ALTER TABLE ""People"" ALTER COLUMN ""Id"" SET GENERATED ALWAYS;");
        }

        [Fact]
        public virtual async Task Alter_column_change_serial_to_identity()
        {
            await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.HasKey("Id");
                    }),
                builder => builder.Entity("People").Property<int>("Id").UseSerialColumn(),
                builder => builder.Entity("People").Property<int>("Id").UseIdentityAlwaysColumn(),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    var column = Assert.Single(table.Columns);
                    Assert.Equal(NpgsqlValueGenerationStrategy.IdentityAlwaysColumn, column[NpgsqlAnnotationNames.ValueGenerationStrategy]);
                    Assert.Null(column[NpgsqlAnnotationNames.IdentityOptions]);
                });

            AssertSql(
                @"ALTER SEQUENCE ""People_Id_seq"" RENAME TO ""People_Id_old_seq"";
ALTER TABLE ""People"" ALTER COLUMN ""Id"" DROP DEFAULT;
ALTER TABLE ""People"" ALTER COLUMN ""Id"" ADD GENERATED ALWAYS AS IDENTITY;
SELECT * FROM setval('""People_Id_seq""', nextval('""People_Id_old_seq""'), false);
DROP SEQUENCE ""People_Id_old_seq"";");
        }

        [Fact]
        public virtual async Task Alter_column_serial_change_type()
        {
            await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id").UseSerialColumn();
                        e.HasKey("Id");
                    }),
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<long>("Id").UseSerialColumn();
                        e.HasKey("Id");
                    }),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    var column = Assert.Single(table.Columns);
                    Assert.Equal(NpgsqlValueGenerationStrategy.SerialColumn, column[NpgsqlAnnotationNames.ValueGenerationStrategy]);
                    Assert.Equal("bigint", column.StoreType);
                    Assert.Null(column[NpgsqlAnnotationNames.IdentityOptions]);
                });

            AssertSql(
                @"ALTER TABLE ""People"" ALTER COLUMN ""Id"" TYPE bigint;");
        }

        [Fact]
        public virtual async Task Alter_column_restart_identity()
        {
            await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id").UseIdentityByDefaultColumn();
                        e.HasKey("Id");
                    }),
                builder => builder.Entity("People").Property<int>("Id").HasIdentityOptions(startValue: 10),
                builder => builder.Entity("People").Property<int>("Id").HasIdentityOptions(startValue: 20),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    var column = Assert.Single(table.Columns);
                    Assert.Equal(NpgsqlValueGenerationStrategy.IdentityByDefaultColumn, column[NpgsqlAnnotationNames.ValueGenerationStrategy]);
                    var options = IdentitySequenceOptionsData.Get(column);
                    Assert.Equal(10, options.StartValue);  // Restarting doesn't change the scaffolded start value
                });

            AssertSql(
                @"ALTER TABLE ""People"" ALTER COLUMN ""Id"" RESTART WITH 20;");
        }

        [Fact]
        public override async Task Alter_column_set_collation()
        {
            await base.Alter_column_set_collation();

            AssertSql(
                @"ALTER TABLE ""People"" ALTER COLUMN ""Name"" TYPE text COLLATE ""POSIX"";");
        }

        [Fact]
        public override async Task Alter_column_reset_collation()
        {
            await base.Alter_column_reset_collation();

            AssertSql(
                @"ALTER TABLE ""People"" ALTER COLUMN ""Name"" TYPE text COLLATE ""default"";");
        }

        [Fact]
        public async Task Alter_column_change_default_column_collation()
        {
            await Test(
                builder => builder.Entity("People", b =>
                {
                    b.Property<int>("Id");
                    b.Property<string>("Name");
                }),
                builder => builder.UseDefaultColumnCollation("POSIX"),
                builder => builder.UseDefaultColumnCollation("C"),
                model =>
                {
                    // var table = Assert.Single(model.Tables);
                    // Assert.Equal(2, table.Columns.Count);
                    // var nameColumn = Assert.Single(table.Columns, c => c.Name == "Name");
                    // Assert.Equal("C", nameColumn.Collation);
                });

            AssertSql(
                @"ALTER TABLE ""People"" ALTER COLUMN ""Name"" TYPE text COLLATE ""C"";");
        }

        public override async Task Drop_column()
        {
            await base.Drop_column();

            AssertSql(
                @"ALTER TABLE ""People"" DROP COLUMN ""SomeColumn"";");
        }

        public override async Task Drop_column_primary_key()
        {
            await base.Drop_column_primary_key();

            AssertSql(
                @"ALTER TABLE ""People"" DROP CONSTRAINT ""PK_People"";",
                //
                @"ALTER TABLE ""People"" DROP COLUMN ""Id"";");
        }

        [Fact]
        public virtual async Task Drop_column_system()
        {
            // System columns (e.g. xmin) are implicitly always present. If an xmin property is removed,
            // nothing should happen.
            await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.Property<uint>("xmin");
                        e.HasKey("Id");
                    }),
                builder => { },
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.HasKey("Id");
                    }),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    Assert.Equal("Id", Assert.Single(table.Columns).Name);
                });

            AssertSql();
        }

        public override async Task Rename_column()
        {
            await base.Rename_column();

            AssertSql(
                @"ALTER TABLE ""People"" RENAME COLUMN ""SomeColumn"" TO ""SomeOtherColumn"";");
        }

        #endregion

        #region Index

        public override async Task Create_index_unique()
        {
            await base.Create_index_unique();

            AssertSql(
                @"CREATE UNIQUE INDEX ""IX_People_FirstName_LastName"" ON ""People"" (""FirstName"", ""LastName"");");
        }

        public override async Task Create_index_with_filter()
        {
            await base.Create_index_with_filter();

            AssertSql(
                @"CREATE INDEX ""IX_People_Name"" ON ""People"" (""Name"") WHERE ""Name"" IS NOT NULL;");
        }

        public override async Task Create_unique_index_with_filter()
        {
            await base.Create_unique_index_with_filter();

            AssertSql(
                @"CREATE UNIQUE INDEX ""IX_People_Name"" ON ""People"" (""Name"") WHERE ""Name"" IS NOT NULL AND ""Name"" <> '';");
        }

        [Fact]
        public virtual async Task Create_index_with_include()
        {
            await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.Property<string>("FirstName");
                        e.Property<string>("LastName").HasColumnName("last_name");
                        e.Property<string>("Name");
                    }),
                builder => { },
                builder => builder.Entity("People").HasIndex("Name")
                    .IncludeProperties("FirstName", "LastName"),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    var index = Assert.Single(table.Indexes);
                    Assert.Equal(1, index.Columns.Count);
                    Assert.Contains(table.Columns.Single(c => c.Name == "Name"), index.Columns);
                    var includedColumns = (string[])index[NpgsqlAnnotationNames.IndexInclude]!;
                    if (TestEnvironment.PostgresVersion.AtLeast(11))
                    {
                        Assert.Contains("FirstName", includedColumns);
                        Assert.Contains("last_name", includedColumns);
                    }
                    else
                        Assert.Null(includedColumns);
                });

            AssertSql(TestEnvironment.PostgresVersion.AtLeast(11)
                ? @"CREATE INDEX ""IX_People_Name"" ON ""People"" (""Name"") INCLUDE (""FirstName"", last_name);"
                : @"CREATE INDEX ""IX_People_Name"" ON ""People"" (""Name"");");
        }

        [Fact]
        public virtual async Task Create_index_with_include_and_filter()
        {
            await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.Property<string>("FirstName");
                        e.Property<string>("LastName");
                        e.Property<string>("Name");
                    }),
                builder => { },
                builder => builder.Entity("People").HasIndex("Name")
                    .IncludeProperties("FirstName", "LastName")
                    .HasFilter(@"""Name"" IS NOT NULL"),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    var index = Assert.Single(table.Indexes);
                    Assert.Equal(@"(""Name"" IS NOT NULL)", index.Filter);
                    Assert.Equal(1, index.Columns.Count);
                    Assert.Contains(table.Columns.Single(c => c.Name == "Name"), index.Columns);
                    var includedColumns = (string[])index[NpgsqlAnnotationNames.IndexInclude]!;
                    if (TestEnvironment.PostgresVersion.AtLeast(11))
                    {
                        Assert.Contains("FirstName", includedColumns);
                        Assert.Contains("LastName", includedColumns);
                    }
                    else
                        Assert.Null(includedColumns);
                });

            AssertSql(TestEnvironment.PostgresVersion.AtLeast(11)
                ? @"CREATE INDEX ""IX_People_Name"" ON ""People"" (""Name"") INCLUDE (""FirstName"", ""LastName"") WHERE ""Name"" IS NOT NULL;"
                : @"CREATE INDEX ""IX_People_Name"" ON ""People"" (""Name"") WHERE ""Name"" IS NOT NULL;");
        }

        [Fact]
        public virtual async Task Create_index_unique_with_include()
        {
            await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.Property<string>("FirstName");
                        e.Property<string>("LastName");
                        e.Property<string>("Name").IsRequired();
                    }),
                builder => { },
                builder => builder.Entity("People").HasIndex("Name")
                    .IsUnique()
                    .IncludeProperties("FirstName", "LastName"),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    var index = Assert.Single(table.Indexes);
                    Assert.True(index.IsUnique);
                    Assert.Equal(1, index.Columns.Count);
                    Assert.Contains(table.Columns.Single(c => c.Name == "Name"), index.Columns);
                    var includedColumns = (string[])index[NpgsqlAnnotationNames.IndexInclude]!;
                    if (TestEnvironment.PostgresVersion.AtLeast(11))
                    {
                        Assert.Contains("FirstName", includedColumns);
                        Assert.Contains("LastName", includedColumns);
                    }
                    else
                        Assert.Null(includedColumns);
                });

            AssertSql(TestEnvironment.PostgresVersion.AtLeast(11)
                ? @"CREATE UNIQUE INDEX ""IX_People_Name"" ON ""People"" (""Name"") INCLUDE (""FirstName"", ""LastName"");"
                : @"CREATE UNIQUE INDEX ""IX_People_Name"" ON ""People"" (""Name"");");
        }

        [Fact]
        public virtual async Task Create_index_unique_with_include_and_filter()
        {
            await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.Property<string>("FirstName");
                        e.Property<string>("LastName");
                        e.Property<string>("Name").IsRequired();
                    }),
                builder => { },
                builder => builder.Entity("People").HasIndex("Name")
                    .IsUnique()
                    .IncludeProperties("FirstName", "LastName")
                    .HasFilter(@"""Name"" IS NOT NULL"),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    var index = Assert.Single(table.Indexes);
                    Assert.True(index.IsUnique);
                    Assert.Equal(@"(""Name"" IS NOT NULL)", index.Filter);
                    Assert.Equal(1, index.Columns.Count);
                    Assert.Contains(table.Columns.Single(c => c.Name == "Name"), index.Columns);
                    var includedColumns = (string[])index[NpgsqlAnnotationNames.IndexInclude]!;
                    if (TestEnvironment.PostgresVersion.AtLeast(11))
                    {
                        Assert.Contains("FirstName", includedColumns);
                        Assert.Contains("LastName", includedColumns);
                    }
                    else
                        Assert.Null(includedColumns);
                });

            AssertSql(TestEnvironment.PostgresVersion.AtLeast(11)
                    ? @"CREATE UNIQUE INDEX ""IX_People_Name"" ON ""People"" (""Name"") INCLUDE (""FirstName"", ""LastName"") WHERE ""Name"" IS NOT NULL;"
                    : @"CREATE UNIQUE INDEX ""IX_People_Name"" ON ""People"" (""Name"") WHERE ""Name"" IS NOT NULL;");
        }

        [Fact]
        public virtual async Task Create_index_concurrently()
        {
            await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.Property<int>("Age");
                    }),
                builder => { },
                builder => builder.Entity("People").HasIndex("Age")
                    .IsCreatedConcurrently(),
                asserter: null); // No scaffolding for IsCreatedConcurrently

            AssertSql(
                @"CREATE INDEX CONCURRENTLY ""IX_People_Age"" ON ""People"" (""Age"");");
        }

        [Fact]
        public virtual async Task Create_index_with_method()
        {
            await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.Property<int>("Age");
                    }),
                builder => { },
                builder => builder.Entity("People").HasIndex("Age")
                    .HasMethod("hash"),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    var index = Assert.Single(table.Indexes);
                    Assert.Equal("hash", index[NpgsqlAnnotationNames.IndexMethod]);
                });

            AssertSql(
                @"CREATE INDEX ""IX_People_Age"" ON ""People"" USING hash (""Age"");");
        }

        [Fact]
        public virtual async Task Create_index_with_operators()
        {
            await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.Property<string>("FirstName");
                        e.Property<string>("LastName");
                    }),
                builder => { },
                builder => builder.Entity("People").HasIndex("FirstName", "LastName")
                    .HasOperators("text_pattern_ops"),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    var index = Assert.Single(table.Indexes);
                    Assert.Equal(new[] { "text_pattern_ops", null }, index[NpgsqlAnnotationNames.IndexOperators]);
                });

            AssertSql(
                @"CREATE INDEX ""IX_People_FirstName_LastName"" ON ""People"" (""FirstName"" text_pattern_ops, ""LastName"");");
        }

        // Index collation: which collations are available on a given PostgreSQL varies (e.g. Linux vs. Windows),
        // so we test support for this on the generated SQL only, in NpgsqlMigrationSqlGeneratorTest, and not against
        // the database here.

        [Fact]
        public virtual async Task Create_index_with_sort_order()
        {
            await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.Property<string>("FirstName");
                        e.Property<string>("LastName");
                    }),
                builder => { },
                builder => builder.Entity("People").HasIndex("FirstName", "LastName")
                    .HasSortOrder(SortOrder.Descending, SortOrder.Ascending),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    var index = Assert.Single(table.Indexes);
                    Assert.Equal(new[] { SortOrder.Descending, SortOrder.Ascending },
                        index[NpgsqlAnnotationNames.IndexSortOrder]);
                });

            AssertSql(
                @"CREATE INDEX ""IX_People_FirstName_LastName"" ON ""People"" (""FirstName"" DESC, ""LastName"");");
        }

        [Fact]
        public virtual async Task Create_index_with_null_sort_order()
        {
            await Test(
                builder => builder.Entity(
                    "People", e =>
                    {
                        e.Property<int>("Id");
                        e.Property<string>("FirstName");
                        e.Property<string>("MiddleName");
                        e.Property<string>("LastName");
                    }),
                builder => { },
                builder => builder.Entity("People").HasIndex("FirstName", "MiddleName", "LastName")
                    .HasNullSortOrder(NullSortOrder.NullsFirst, NullSortOrder.Unspecified, NullSortOrder.NullsLast),
                model =>
                {
                    var table = Assert.Single(model.Tables);
                    var index = Assert.Single(table.Indexes);
                    Assert.Equal(new[] { NullSortOrder.NullsFirst, NullSortOrder.NullsLast, NullSortOrder.NullsLast },
                        index[NpgsqlAnnotationNames.IndexNullSortOrder]);
                });

            AssertSql(
                @"CREATE INDEX ""IX_People_FirstName_MiddleName_LastName"" ON ""People"" (""FirstName"" NULLS FIRST, ""MiddleName"", ""LastName"" NULLS LAST);");
        }

        [Fact]
        public virtual async Task Create_index_tsvector()
        {
            await Test(
                builder => builder.Entity(
                    "Blogs", e =>
                    {
                        e.Property<string>("Title").IsRequired();
                        e.Property<string>("Description");
                    }),
                builder => { },
                builder => builder.Entity("Blogs")
                    .HasIndex("Title", "Description")
                    .IsTsVectorExpressionIndex("simple"),
                model => { });

            AssertSql(
                @"CREATE INDEX ""IX_Blogs_Title_Description"" ON ""Blogs"" (to_tsvector('simple', ""Title"" || ' ' || coalesce(""Description"", '')));");
        }

        [Fact]
        public virtual async Task Create_index_tsvector_using_gin()
        {
            await Test(
                builder => builder.Entity(
                    "Blogs", e =>
                    {
                        e.Property<string>("Title").IsRequired();
                        e.Property<string>("Description");
                    }),
                builder => { },
                builder => builder.Entity("Blogs")
                    .HasIndex("Title", "Description")
                    .HasMethod("GIN")
                    .IsTsVectorExpressionIndex("simple"),
                model => { });

            AssertSql(
                @"CREATE INDEX ""IX_Blogs_Title_Description"" ON ""Blogs"" USING GIN (to_tsvector('simple', ""Title"" || ' ' || coalesce(""Description"", '')));");
        }

        public override async Task Drop_index()
        {
            await base.Drop_index();

            AssertSql(
                @"DROP INDEX ""IX_People_SomeField"";");
        }

        public override async Task Rename_index()
        {
            await base.Rename_index();

            AssertSql(
                @"ALTER INDEX ""Foo"" RENAME TO foo;");
        }

        #endregion

        #region Key and constraint

        public override async Task Add_primary_key()
        {
            await base.Add_primary_key();

            AssertSql(
                @"ALTER TABLE ""People"" ADD CONSTRAINT ""PK_People"" PRIMARY KEY (""SomeField"");");
        }

        public override async Task Add_primary_key_with_name()
        {
            await base.Add_primary_key_with_name();

            AssertSql(
                @"ALTER TABLE ""People"" ADD CONSTRAINT ""PK_Foo"" PRIMARY KEY (""SomeField"");");
        }

        public override async Task Add_primary_key_composite_with_name()
        {
            await base.Add_primary_key_composite_with_name();

            AssertSql(
                @"ALTER TABLE ""People"" ADD CONSTRAINT ""PK_Foo"" PRIMARY KEY (""SomeField1"", ""SomeField2"");");
        }

        public override async Task Drop_primary_key()
        {
            await base.Drop_primary_key();

            AssertSql(
                @"ALTER TABLE ""People"" DROP CONSTRAINT ""PK_People"";");
        }

        public override Task Add_foreign_key()
            => Task.CompletedTask; // https://github.com/npgsql/efcore.pg/issues/1217

        public override async Task Add_foreign_key_with_name()
        {
            await base.Add_foreign_key_with_name();

            AssertSql(
                @"ALTER TABLE ""Orders"" ADD CONSTRAINT ""FK_Foo"" FOREIGN KEY (""CustomerId"") REFERENCES ""Customers"" (""Id"") ON DELETE RESTRICT;");
        }

        public override async Task Drop_foreign_key()
        {
            await base.Drop_foreign_key();

            AssertSql(
                @"ALTER TABLE ""Orders"" DROP CONSTRAINT ""FK_Orders_Customers_CustomerId"";");
        }

        public override async Task Add_unique_constraint()
        {
            await base.Add_unique_constraint();

            AssertSql(
                @"ALTER TABLE ""People"" ADD CONSTRAINT ""AK_People_AlternateKeyColumn"" UNIQUE (""AlternateKeyColumn"");");
        }

        public override async Task Add_unique_constraint_composite_with_name()
        {
            await base.Add_unique_constraint_composite_with_name();

            AssertSql(
                @"ALTER TABLE ""People"" ADD CONSTRAINT ""AK_Foo"" UNIQUE (""AlternateKeyColumn1"", ""AlternateKeyColumn2"");");
        }

        public override async Task Drop_unique_constraint()
        {
            await base.Drop_unique_constraint();

            AssertSql(
                @"ALTER TABLE ""People"" DROP CONSTRAINT ""AK_People_AlternateKeyColumn"";");
        }

        public override async Task Add_check_constraint_with_name()
        {
            await base.Add_check_constraint_with_name();

            AssertSql(
                @"ALTER TABLE ""People"" ADD CONSTRAINT ""CK_Foo"" CHECK (""DriverLicense"" > 0);");
        }

        public override async Task Drop_check_constraint()
        {
            await base.Drop_check_constraint();

            AssertSql(
                @"ALTER TABLE ""People"" DROP CONSTRAINT ""CK_Foo"";");
        }

        #endregion

        #region Sequence

        public override async Task Create_sequence()
        {
            await base.Create_sequence();

            AssertSql(
                @"CREATE SEQUENCE ""TestSequence"" AS integer START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE NO CYCLE;");
        }

        public override async Task Create_sequence_all_settings()
        {
            await base.Create_sequence_all_settings();

            AssertSql(
                @"CREATE SCHEMA IF NOT EXISTS dbo2;",
                //
                @"CREATE SEQUENCE dbo2.""TestSequence"" START WITH 3 INCREMENT BY 2 MINVALUE 2 MAXVALUE 916 CYCLE;");
        }

        [Fact]
        public virtual async Task Create_sequence_smallint()
        {
            await Test(
                builder => { },
                builder => builder.HasSequence<short>("TestSequence"),
                model =>
                {
                    var sequence = Assert.Single(model.Sequences);
                    Assert.Equal("TestSequence", sequence.Name);
                    Assert.Equal("smallint", sequence.StoreType);
                });

            AssertSql(
                @"CREATE SEQUENCE ""TestSequence"" AS smallint START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE NO CYCLE;");
        }

        [Fact]
        public override async Task Alter_sequence_all_settings()
        {
            await Test(
                builder => builder.HasSequence<int>("foo"),
                builder => { },
                builder => builder.HasSequence<int>("foo")
                    .StartsAt(-3)
                    .IncrementsBy(2)
                    .HasMin(-5)
                    .HasMax(10)
                    .IsCyclic(),
                model =>
                {
                    var sequence = Assert.Single(model.Sequences);
                    Assert.Equal(1, sequence.StartValue); // Restarting doesn't change the scaffolded start value
                    Assert.Equal(2, sequence.IncrementBy);
                    Assert.Equal(-5, sequence.MinValue);
                    Assert.Equal(10, sequence.MaxValue);
                    Assert.True(sequence.IsCyclic);
                });

            AssertSql(
                @"ALTER SEQUENCE foo INCREMENT BY 2 MINVALUE -5 MAXVALUE 10 CYCLE;",
                //
                @"ALTER SEQUENCE foo RESTART WITH -3;");
        }

        public override async Task Alter_sequence_increment_by()
        {
            await base.Alter_sequence_increment_by();

            AssertSql(
                @"ALTER SEQUENCE foo INCREMENT BY 2 NO MINVALUE NO MAXVALUE NO CYCLE;");
        }

        public override async Task Drop_sequence()
        {
            await base.Drop_sequence();

            AssertSql(
                @"DROP SEQUENCE ""TestSequence"";");
        }

        public override async Task Rename_sequence()
        {
            await base.Rename_sequence();

            AssertSql(
                @"ALTER SEQUENCE ""TestSequence"" RENAME TO testsequence;");
        }

        public override async Task Move_sequence()
        {
            await base.Move_sequence();

            AssertSql(
                @"CREATE SCHEMA IF NOT EXISTS ""TestSequenceSchema"";",
                //
                @"ALTER SEQUENCE ""TestSequence"" SET SCHEMA ""TestSequenceSchema"";");
        }

        #endregion

        #region Data seeding

        public override async Task InsertDataOperation()
        {
            await base.InsertDataOperation();

            AssertSql(
                @"INSERT INTO ""Person"" (""Id"", ""Name"")
VALUES (1, 'Daenerys Targaryen');
INSERT INTO ""Person"" (""Id"", ""Name"")
VALUES (2, 'John Snow');
INSERT INTO ""Person"" (""Id"", ""Name"")
VALUES (3, 'Arya Stark');
INSERT INTO ""Person"" (""Id"", ""Name"")
VALUES (4, 'Harry Strickland');
INSERT INTO ""Person"" (""Id"", ""Name"")
VALUES (5, NULL);");
        }

        public override async Task DeleteDataOperation_simple_key()
        {
            await base.DeleteDataOperation_simple_key();

            AssertSql(
                @"DELETE FROM ""Person""
WHERE ""Id"" = 2;");
        }

        public override async Task DeleteDataOperation_composite_key()
        {
            await base.DeleteDataOperation_composite_key();

            AssertSql(
                @"DELETE FROM ""Person""
WHERE ""AnotherId"" = 12 AND ""Id"" = 2;");
        }

        public override async Task UpdateDataOperation_simple_key()
        {
            await base.UpdateDataOperation_simple_key();

            AssertSql(
                @"UPDATE ""Person"" SET ""Name"" = 'Another John Snow'
WHERE ""Id"" = 2;");
        }

        public override async Task UpdateDataOperation_composite_key()
        {
            await base.UpdateDataOperation_composite_key();

            AssertSql(
                @"UPDATE ""Person"" SET ""Name"" = 'Another John Snow'
WHERE ""AnotherId"" = 11 AND ""Id"" = 2;");
        }

        public override async Task UpdateDataOperation_multiple_columns()
        {
            await base.UpdateDataOperation_multiple_columns();

            AssertSql(
                @"UPDATE ""Person"" SET ""Age"" = 21, ""Name"" = 'Another John Snow'
WHERE ""Id"" = 2;");
        }

        [ConditionalFact]
        public virtual async Task InsertDataOperation_restarts_identity()
        {
            await Test(
                builder =>
                {
                    builder.Entity(
                        "Person", e =>
                        {
                            e.Property<int>("Id").UseIdentityByDefaultColumn();
                            e.Property<string>("Name");
                            e.HasKey("Id");
                        });
                    builder.Entity(
                        "Person2", e =>
                        {
                            e.Property<int>("Id").UseIdentityByDefaultColumn();
                            e.Property<string>("Name");
                            e.HasKey("Id");
                        });
                },
                builder => { },
                builder =>
                {
                    builder.Entity("Person").HasData(
                        new { Id = 1, Name = "Daenerys Targaryen" },
                        new { Id = 2, Name = "John Snow"});
                    builder.Entity("Person2").HasData(
                        new { Id = -10, Name = "Daenerys Targaryen" },
                        new { Id = -20, Name = "John Snow"});
                },
                model => { });

            AssertSql(
                @"INSERT INTO ""Person"" (""Id"", ""Name"")
VALUES (1, 'Daenerys Targaryen');
INSERT INTO ""Person"" (""Id"", ""Name"")
VALUES (2, 'John Snow');",
                //
                @"INSERT INTO ""Person2"" (""Id"", ""Name"")
VALUES (-10, 'Daenerys Targaryen');
INSERT INTO ""Person2"" (""Id"", ""Name"")
VALUES (-20, 'John Snow');",
                //
                @"SELECT setval(
    pg_get_serial_sequence('""Person""', 'Id'),
    GREATEST(
        (SELECT MAX(""Id"") FROM ""Person"") + 1,
        nextval(pg_get_serial_sequence('""Person""', 'Id'))),
    false);
SELECT setval(
    pg_get_serial_sequence('""Person2""', 'Id'),
    GREATEST(
        (SELECT MAX(""Id"") FROM ""Person2"") + 1,
        nextval(pg_get_serial_sequence('""Person2""', 'Id'))),
    false);");
        }

        #endregion

        #region PostgreSQL extensions

        [Fact]
        public virtual async Task Ensure_postgres_extension()
        {
            await Test(
                builder => { },
                builder => builder.HasPostgresExtension("citext"),
                model =>
                {
                    var citext = Assert.Single(model.GetPostgresExtensions());
                    Assert.Equal("citext", citext.Name);
                    Assert.Equal("public", citext.Schema);
                });

            AssertSql(
                @"CREATE EXTENSION IF NOT EXISTS citext;");
        }

        [Fact]
        public virtual async Task Ensure_postgres_extension_with_schema()
        {
            await Test(
                _ => { },
                builder => builder.HasPostgresExtension("some_schema", "citext"),
                model =>
                {
                    var citext = Assert.Single(model.GetPostgresExtensions());
                    Assert.Equal("citext", citext.Name);
                    Assert.Equal("some_schema", citext.Schema);
                });

            AssertSql(
                @"CREATE SCHEMA IF NOT EXISTS some_schema;",
                //
                @"CREATE EXTENSION IF NOT EXISTS citext SCHEMA some_schema;");
        }

        #endregion

        #region PostgreSQL enums

        [Fact]
        public virtual async Task Create_enum()
        {
            await Test(
                builder => { },
                builder => builder.HasPostgresEnum("Mood", new[] { "Happy", "Sad" }),
                model =>
                {
                    var moodEnum = Assert.Single(model.GetPostgresEnums());
                    Assert.Equal("Mood", moodEnum.Name);
                    Assert.Null(moodEnum.Schema);
                    Assert.Collection(moodEnum.Labels,
                        l => Assert.Equal("Happy", l),
                        l => Assert.Equal("Sad", l));
                });

            AssertSql(
                @"CREATE TYPE ""Mood"" AS ENUM ('Happy', 'Sad');");
        }

        [Fact]
        public virtual async Task Create_enum_with_schema()
        {
            await Test(
                builder => { },
                builder => builder.HasPostgresEnum("some_schema", "Mood", new[] { "Happy", "Sad" }),
                model =>
                {
                    var moodEnum = Assert.Single(model.GetPostgresEnums());
                    Assert.Equal("Mood", moodEnum.Name);
                    Assert.Equal("some_schema", moodEnum.Schema);
                    Assert.Collection(moodEnum.Labels,
                        l => Assert.Equal("Happy", l),
                        l => Assert.Equal("Sad", l));
                });

            AssertSql(
                @"CREATE SCHEMA IF NOT EXISTS some_schema;",
                //
                @"CREATE TYPE some_schema.""Mood"" AS ENUM ('Happy', 'Sad');");
        }

        [Fact]
        public virtual async Task Drop_enum()
        {
            await Test(
                builder => builder.HasPostgresEnum("Mood", new[] { "Happy", "Sad" }),
                builder => { },
                model => Assert.Empty(model.GetPostgresEnums()));

            AssertSql(
                @"DROP TYPE ""Mood"";");
        }

        [Fact] // #979
        public virtual async Task Do_not_alter_existing_enum_when_creating_new_one()
        {
            await Test(
                builder => builder.HasPostgresEnum("Enum1", new[] { "A", "B" }),
                builder => { },
                builder => builder.HasPostgresEnum("Enum2", new[] { "X", "Y" }),
                model => Assert.Equal(2, model.GetPostgresEnums().Count()));

            AssertSql(
                @"CREATE TYPE ""Enum2"" AS ENUM ('X', 'Y');");
        }

        [Fact]
        public virtual async Task Alter_enum_add_label_at_end()
        {
            await Test(
                builder => builder.HasPostgresEnum("Mood", new[] { "Happy", "Sad" }),
                builder => builder.HasPostgresEnum("Mood", new[] { "Happy", "Sad", "Angry" }),
                model =>
                {
                    var moodEnum = Assert.Single(model.GetPostgresEnums());
                    Assert.Collection(moodEnum.Labels,
                        l => Assert.Equal("Happy", l),
                        l => Assert.Equal("Sad", l),
                        l => Assert.Equal("Angry", l));
                });

            AssertSql(
                @"ALTER TYPE ""Mood"" ADD VALUE 'Angry';");
        }

        [Fact]
        public virtual async Task Alter_enum_add_label_in_middle()
        {
            await Test(
                builder => builder.HasPostgresEnum("Mood", new[] { "Happy", "Sad" }),
                builder => builder.HasPostgresEnum("Mood", new[] { "Happy", "Angry", "Sad" }),
                model =>
                {
                    var moodEnum = Assert.Single(model.GetPostgresEnums());
                    Assert.Collection(moodEnum.Labels,
                        l => Assert.Equal("Happy", l),
                        l => Assert.Equal("Angry", l),
                        l => Assert.Equal("Sad", l));
                });

            AssertSql(
                @"ALTER TYPE ""Mood"" ADD VALUE 'Angry' BEFORE 'Sad';");
        }

        [Fact]
        public virtual Task Alter_enum_drop_label_not_supported()
            => TestThrows<NotSupportedException>(
                builder => builder.HasPostgresEnum("Mood", new[] { "Happy", "Sad" }),
                builder => builder.HasPostgresEnum("Mood", new[] { "Happy" }));

        [Fact]
        public virtual Task Alter_enum_change_label_not_supported()
            => TestThrows<NotSupportedException>(
                builder => builder.HasPostgresEnum("Mood", new[] { "Happy", "Sad" }),
                builder => builder.HasPostgresEnum("Mood", new[] { "Happy", "Angry" }));

        #endregion

        #region PostgreSQL collation management

        [Fact]
        public virtual async Task Create_collation()
        {
            await Test(
                builder => { },
                builder => builder.HasCollation("dummy", locale: "POSIX", provider: "libc"),
                model =>
                {
                    var collation = Assert.Single(PostgresCollation.GetCollations(model));

                    Assert.Equal("dummy", collation.Name);
                    Assert.Equal("libc", collation.Provider);
                    Assert.Equal("POSIX", collation.LcCollate);
                    Assert.Equal("POSIX", collation.LcCtype);
                    Assert.True(collation.IsDeterministic);
                });

            AssertSql(
                @"CREATE COLLATION dummy (LC_COLLATE = 'POSIX',
    LC_CTYPE = 'POSIX',
    PROVIDER = libc
);");
        }

        [ConditionalFact]
        [MinimumPostgresVersion(12, 0)]
        public virtual async Task Create_collation_non_deterministic()
        {
            await Test(
                builder => { },
                builder => builder.HasCollation("some_collation", locale: "en-u-ks-primary", provider: "icu", deterministic: false),
                model =>
                {
                    var collation = Assert.Single(PostgresCollation.GetCollations(model));

                    Assert.Equal("some_collation", collation.Name);
                    Assert.Equal("icu", collation.Provider);
                    Assert.Equal("en-u-ks-primary", collation.LcCollate);
                    Assert.Equal("en-u-ks-primary", collation.LcCtype);
                    Assert.False(collation.IsDeterministic);
                });

            AssertSql(
                @"CREATE COLLATION some_collation (LC_COLLATE = 'en-u-ks-primary',
    LC_CTYPE = 'en-u-ks-primary',
    PROVIDER = icu,
    DETERMINISTIC = False
);");
        }

        [Fact]
        public virtual async Task Drop_collation()
        {
            await Test(
                builder => builder.HasCollation("dummy", locale: "POSIX", provider: "libc"),
                builder => { },
                model => Assert.Empty(PostgresCollation.GetCollations(model)));

            AssertSql(
                @"DROP COLLATION dummy;");
        }

        [Fact]
        public virtual Task Alter_collation_throws()
            => TestThrows<NotSupportedException>(
                builder => builder.HasCollation("dummy", locale: "POSIX", provider: "libc"),
                builder => builder.HasCollation("dummy", locale: "C", provider: "libc"));

        #endregion PostgreSQL collation management

        protected override string NonDefaultCollation => "POSIX";

        public class MigrationsNpgsqlFixture : MigrationsFixtureBase
        {
            protected override string StoreName { get; } = nameof(MigrationsNpgsqlTest);
            protected override ITestStoreFactory TestStoreFactory => NpgsqlTestStoreFactory.Instance;
            public override TestHelpers TestHelpers => NpgsqlTestHelpers.Instance;

            protected override IServiceCollection AddServices(IServiceCollection serviceCollection)
                => base.AddServices(serviceCollection)
                    .AddScoped<IDatabaseModelFactory, NpgsqlDatabaseModelFactory>();

            public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
            {
                new NpgsqlDbContextOptionsBuilder(base.AddOptions(builder)
                        // Some tests create expression indexes, but these cannot be reverse-engineered.
                        .ConfigureWarnings(
                            w => { w.Ignore(NpgsqlEventId.ExpressionIndexSkippedWarning); }))
                    // Various migration operations PG-version sensitive, configure the context with the actual version
                    // we're connecting to.
                    .SetPostgresVersion(TestEnvironment.PostgresVersion);

                return builder;
            }
        }
    }
}
