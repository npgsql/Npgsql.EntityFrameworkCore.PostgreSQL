using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations.Operations;
using Npgsql.EntityFrameworkCore.PostgreSQL.TestUtilities;
using Xunit;

namespace Npgsql.EntityFrameworkCore.PostgreSQL.Migrations
{
    public class NpgsqlMigrationsSqlGeneratorTest : MigrationsSqlGeneratorTestBase
    {
        #region Database

        [Fact]
        public virtual void CreateDatabaseOperation()
        {
            Generate(new NpgsqlCreateDatabaseOperation { Name = "Northwind" });

            AssertSql(
                @"CREATE DATABASE ""Northwind"";
");
        }

        [ConditionalFact]
        public virtual void CreateDatabaseOperation_with_collation()
        {
            Generate(
                new NpgsqlCreateDatabaseOperation { Name = "Northwind", Collation = "POSIX" });

            AssertSql(
                @"CREATE DATABASE ""Northwind""
LC_COLLATE ""POSIX"";
");
        }

        [Fact]
        public virtual void CreateDatabaseOperation_with_template()
        {
            Generate(new NpgsqlCreateDatabaseOperation
            {
                Name = "Northwind",
                Template = "MyTemplate"
            });

            AssertSql(
                @"CREATE DATABASE ""Northwind""
TEMPLATE ""MyTemplate"";
");
        }

        [Fact]
        public virtual void CreateDatabaseOperation_with_tablespace()
        {
            Generate(new NpgsqlCreateDatabaseOperation
            {
                Name = "some_db",
                Tablespace = "MyTablespace"
            });

            AssertSql(
                @"CREATE DATABASE some_db
TABLESPACE ""MyTablespace"";
");
        }

        #endregion

        public override void AddColumnOperation_without_column_type()
        {
            base.AddColumnOperation_without_column_type();

            AssertSql(
                @"ALTER TABLE ""People"" ADD ""Alias"" text NOT NULL;
");
        }

        public override void AddColumnOperation_with_unicode_overridden()
        {
            base.AddColumnOperation_with_unicode_overridden();

            AssertSql(
                @"ALTER TABLE ""Person"" ADD ""Name"" text NULL;
");
        }

        public override void AddColumnOperation_with_unicode_no_model()
        {
            base.AddColumnOperation_with_unicode_no_model();

            AssertSql(
                @"ALTER TABLE ""Person"" ADD ""Name"" text NULL;
");
        }

        public override void AddColumnOperation_with_fixed_length_no_model()
        {
            base.AddColumnOperation_with_fixed_length_no_model();

            AssertSql(
                @"ALTER TABLE ""Person"" ADD ""Name"" character(100) NULL;
");
        }

        public override void AddColumnOperation_with_maxLength_overridden()
        {
            base.AddColumnOperation_with_maxLength_overridden();

            AssertSql(
                @"ALTER TABLE ""Person"" ADD ""Name"" character varying(32) NULL;
");
        }

        public override void AddColumnOperation_with_maxLength_no_model()
        {
            base.AddColumnOperation_with_maxLength_no_model();

            AssertSql(
                @"ALTER TABLE ""Person"" ADD ""Name"" character varying(30) NULL;
");
        }

        public override void AddColumnOperation_with_precision_and_scale_overridden()
        {
            base.AddColumnOperation_with_precision_and_scale_overridden();

            AssertSql(
                @"ALTER TABLE ""Person"" ADD ""Pi"" numeric(15,10) NOT NULL;
");
        }

        public override void AddColumnOperation_with_precision_and_scale_no_model()
        {
            base.AddColumnOperation_with_precision_and_scale_no_model();

            AssertSql(
                @"ALTER TABLE ""Person"" ADD ""Pi"" numeric(20,7) NOT NULL;
");
        }

        public override void AddForeignKeyOperation_without_principal_columns()
        {
            base.AddForeignKeyOperation_without_principal_columns();

            AssertSql(
                @"ALTER TABLE ""People"" ADD FOREIGN KEY (""SpouseId"") REFERENCES ""People"";
");
        }

        public override void AlterColumnOperation_without_column_type()
        {
            base.AlterColumnOperation_without_column_type();

            AssertSql(
                @"ALTER TABLE ""People"" ALTER COLUMN ""LuckyNumber"" TYPE integer;
");
        }

        public override void RenameTableOperation_legacy()
        {
            base.RenameTableOperation_legacy();

            AssertSql(
                @"ALTER TABLE dbo.""People"" RENAME TO ""Person"";
");
        }

        public override void RenameTableOperation()
        {
            base.RenameTableOperation();

            AssertSql(
                @"ALTER TABLE dbo.""People"" RENAME TO ""Person"";
");
        }

        public override void SqlOperation()
        {
            base.SqlOperation();

            AssertSql(
                @"-- I <3 DDL
");
        }

        public override void InsertDataOperation_all_args_spatial()
        {
            base.InsertDataOperation_all_args_spatial();

            AssertSql(
                @"INSERT INTO dbo.""People"" (""Id"", ""Full Name"", ""Geometry"")
VALUES (0, NULL, NULL);
INSERT INTO dbo.""People"" (""Id"", ""Full Name"", ""Geometry"")
VALUES (1, 'Daenerys Targaryen', NULL);
INSERT INTO dbo.""People"" (""Id"", ""Full Name"", ""Geometry"")
VALUES (2, 'John Snow', NULL);
INSERT INTO dbo.""People"" (""Id"", ""Full Name"", ""Geometry"")
VALUES (3, 'Arya Stark', NULL);
INSERT INTO dbo.""People"" (""Id"", ""Full Name"", ""Geometry"")
VALUES (4, 'Harry Strickland', NULL);
INSERT INTO dbo.""People"" (""Id"", ""Full Name"", ""Geometry"")
VALUES (5, 'The Imp', NULL);
INSERT INTO dbo.""People"" (""Id"", ""Full Name"", ""Geometry"")
VALUES (6, 'The Kingslayer', NULL);
INSERT INTO dbo.""People"" (""Id"", ""Full Name"", ""Geometry"")
VALUES (7, 'Aemon Targaryen', GEOMETRY 'SRID=4326;GEOMETRYCOLLECTION Z(LINESTRING Z(1.1 2.2 NaN, 2.2 2.2 NaN, 2.2 1.1 NaN, 7.1 7.2 NaN), LINESTRING Z(7.1 7.2 NaN, 20.2 20.2 NaN, 20.2 1.1 NaN, 70.1 70.2 NaN), MULTIPOINT Z((1.1 2.2 NaN), (2.2 2.2 NaN), (2.2 1.1 NaN)), POLYGON Z((1.1 2.2 NaN, 2.2 2.2 NaN, 2.2 1.1 NaN, 1.1 2.2 NaN)), POLYGON Z((10.1 20.2 NaN, 20.2 20.2 NaN, 20.2 10.1 NaN, 10.1 20.2 NaN)), POINT Z(1.1 2.2 3.3), MULTILINESTRING Z((1.1 2.2 NaN, 2.2 2.2 NaN, 2.2 1.1 NaN, 7.1 7.2 NaN), (7.1 7.2 NaN, 20.2 20.2 NaN, 20.2 1.1 NaN, 70.1 70.2 NaN)), MULTIPOLYGON Z(((10.1 20.2 NaN, 20.2 20.2 NaN, 20.2 10.1 NaN, 10.1 20.2 NaN)), ((1.1 2.2 NaN, 2.2 2.2 NaN, 2.2 1.1 NaN, 1.1 2.2 NaN))))');
");
        }

        public override void InsertDataOperation_required_args()
        {
            base.InsertDataOperation_required_args();

            AssertSql(
                @"INSERT INTO ""People"" (""First Name"")
VALUES ('John');
");
        }

        public override void InsertDataOperation_required_args_composite()
        {
            base.InsertDataOperation_required_args_composite();

            AssertSql(
                @"INSERT INTO ""People"" (""First Name"", ""Last Name"")
VALUES ('John', 'Snow');
");
        }

        public override void InsertDataOperation_required_args_multiple_rows()
        {
            base.InsertDataOperation_required_args_multiple_rows();

            AssertSql(
                @"INSERT INTO ""People"" (""First Name"")
VALUES ('John');
INSERT INTO ""People"" (""First Name"")
VALUES ('Daenerys');
");
        }

        public override void DeleteDataOperation_all_args()
        {
            base.DeleteDataOperation_all_args();

            AssertSql(
                @"DELETE FROM ""People""
WHERE ""First Name"" = 'Hodor';
DELETE FROM ""People""
WHERE ""First Name"" = 'Daenerys';
DELETE FROM ""People""
WHERE ""First Name"" = 'John';
DELETE FROM ""People""
WHERE ""First Name"" = 'Arya';
DELETE FROM ""People""
WHERE ""First Name"" = 'Harry';
");
        }

        public override void DeleteDataOperation_all_args_composite()
        {
            base.DeleteDataOperation_all_args_composite();

            AssertSql(
                @"DELETE FROM ""People""
WHERE ""First Name"" = 'Hodor' AND ""Last Name"" IS NULL;
DELETE FROM ""People""
WHERE ""First Name"" = 'Daenerys' AND ""Last Name"" = 'Targaryen';
DELETE FROM ""People""
WHERE ""First Name"" = 'John' AND ""Last Name"" = 'Snow';
DELETE FROM ""People""
WHERE ""First Name"" = 'Arya' AND ""Last Name"" = 'Stark';
DELETE FROM ""People""
WHERE ""First Name"" = 'Harry' AND ""Last Name"" = 'Strickland';
");
        }

        public override void DeleteDataOperation_required_args()
        {
            base.DeleteDataOperation_required_args();

            AssertSql(
                @"DELETE FROM ""People""
WHERE ""Last Name"" = 'Snow';
");
        }

        public override void DeleteDataOperation_required_args_composite()
        {
            base.DeleteDataOperation_required_args_composite();

            AssertSql(
                @"DELETE FROM ""People""
WHERE ""First Name"" = 'John' AND ""Last Name"" = 'Snow';
");
        }

        public override void UpdateDataOperation_all_args()
        {
            base.UpdateDataOperation_all_args();

            AssertSql(
                @"UPDATE ""People"" SET ""Birthplace"" = 'Winterfell', ""House Allegiance"" = 'Stark', ""Culture"" = 'Northmen'
WHERE ""First Name"" = 'Hodor';
UPDATE ""People"" SET ""Birthplace"" = 'Dragonstone', ""House Allegiance"" = 'Targaryen', ""Culture"" = 'Valyrian'
WHERE ""First Name"" = 'Daenerys';
");
        }

        public override void UpdateDataOperation_all_args_composite()
        {
            base.UpdateDataOperation_all_args_composite();

            AssertSql(
                @"UPDATE ""People"" SET ""House Allegiance"" = 'Stark'
WHERE ""First Name"" = 'Hodor' AND ""Last Name"" IS NULL;
UPDATE ""People"" SET ""House Allegiance"" = 'Targaryen'
WHERE ""First Name"" = 'Daenerys' AND ""Last Name"" = 'Targaryen';
");
        }

        public override void UpdateDataOperation_all_args_composite_multi()
        {
            base.UpdateDataOperation_all_args_composite_multi();

            AssertSql(
                @"UPDATE ""People"" SET ""Birthplace"" = 'Winterfell', ""House Allegiance"" = 'Stark', ""Culture"" = 'Northmen'
WHERE ""First Name"" = 'Hodor' AND ""Last Name"" IS NULL;
UPDATE ""People"" SET ""Birthplace"" = 'Dragonstone', ""House Allegiance"" = 'Targaryen', ""Culture"" = 'Valyrian'
WHERE ""First Name"" = 'Daenerys' AND ""Last Name"" = 'Targaryen';
");
        }

        public override void UpdateDataOperation_all_args_multi()
        {
            base.UpdateDataOperation_all_args_multi();

            AssertSql(
                @"UPDATE ""People"" SET ""Birthplace"" = 'Dragonstone', ""House Allegiance"" = 'Targaryen', ""Culture"" = 'Valyrian'
WHERE ""First Name"" = 'Daenerys';
");
        }

        public override void UpdateDataOperation_required_args()
        {
            base.UpdateDataOperation_required_args();

            AssertSql(
                @"UPDATE ""People"" SET ""House Allegiance"" = 'Targaryen'
WHERE ""First Name"" = 'Daenerys';
");
        }

        public override void UpdateDataOperation_required_args_multiple_rows()
        {
            base.UpdateDataOperation_required_args_multiple_rows();

            AssertSql(
                @"UPDATE ""People"" SET ""House Allegiance"" = 'Stark'
WHERE ""First Name"" = 'Hodor';
UPDATE ""People"" SET ""House Allegiance"" = 'Targaryen'
WHERE ""First Name"" = 'Daenerys';
");
        }

        public override void UpdateDataOperation_required_args_composite()
        {
            base.UpdateDataOperation_required_args_composite();

            AssertSql(
                @"UPDATE ""People"" SET ""House Allegiance"" = 'Targaryen'
WHERE ""First Name"" = 'Daenerys' AND ""Last Name"" = 'Targaryen';
");
        }

        public override void UpdateDataOperation_required_args_composite_multi()
        {
            base.UpdateDataOperation_required_args_composite_multi();

            AssertSql(
                @"UPDATE ""People"" SET ""Birthplace"" = 'Dragonstone', ""House Allegiance"" = 'Targaryen', ""Culture"" = 'Valyrian'
WHERE ""First Name"" = 'Daenerys' AND ""Last Name"" = 'Targaryen';
");
        }

        public override void UpdateDataOperation_required_args_multi()
        {
            base.UpdateDataOperation_required_args_multi();

            AssertSql(
                @"UPDATE ""People"" SET ""Birthplace"" = 'Dragonstone', ""House Allegiance"" = 'Targaryen', ""Culture"" = 'Valyrian'
WHERE ""First Name"" = 'Daenerys';
");
        }

        public override void DefaultValue_with_line_breaks(bool isUnicode)
        {
            // https://github.com/npgsql/efcore.pg/issues/1478
        }

        // Which index collations are available on a given PostgreSQL varies (e.g. Linux vs. Windows)
        // so we test support for this on the generated SQL only, and not against the database in MigrationsNpsqlTest.
        [Fact]
        public void CreateIndexOperation_collation()
        {
            Generate(new CreateIndexOperation
            {
                Name = "IX_People_Name",
                Table = "People",
                Schema = "dbo",
                Columns = new[] { "FirstName", "LastName" },
                [RelationalAnnotationNames.Collation] = new[] { null, "de_DE" }
            });

            AssertSql(
                @"CREATE INDEX ""IX_People_Name"" ON dbo.""People"" (""FirstName"", ""LastName"" COLLATE ""de_DE"");
");
        }

        [Theory]
        [InlineData(MigrationsSqlGenerationOptions.Default)]
        [InlineData(MigrationsSqlGenerationOptions.Idempotent)]
        public void Alter_column_change_serial_to_identity_idempotent(MigrationsSqlGenerationOptions options)
        {
            Generate(
                modelBuilder =>
                {
                    modelBuilder.HasAnnotation(CoreAnnotationNames.ProductVersion, "3.1.0");
                    modelBuilder.Entity<Person>().Property<int>("Id").UseSerialColumn();
                },
                new[]
                {
                    new AlterColumnOperation
                    {
                        Table = "Person",
                        Name = "Id",
                        ClrType = typeof(int),
                        [NpgsqlAnnotationNames.ValueGenerationStrategy] =
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn,

                        OldColumn = new AddColumnOperation
                        {
                            Table = "Person",
                            Name = "Id",
                            ClrType = typeof(int),
                            [NpgsqlAnnotationNames.ValueGenerationStrategy] =
                                NpgsqlValueGenerationStrategy.SerialColumn,
                        }
                    }
                },
                options);

            AssertSql(
                $@"ALTER SEQUENCE ""Person_Id_seq"" RENAME TO ""Person_Id_old_seq"";
ALTER TABLE ""Person"" ALTER COLUMN ""Id"" DROP DEFAULT;
ALTER TABLE ""Person"" ALTER COLUMN ""Id"" ADD GENERATED BY DEFAULT AS IDENTITY;
{(options == MigrationsSqlGenerationOptions.Idempotent ? "PERFORM" : "SELECT")} * FROM setval('""Person_Id_seq""', nextval('""Person_Id_old_seq""'), false);
DROP SEQUENCE ""Person_Id_old_seq"";
");
        }

        #region CockroachDB interleave-in-parent

        // Note that we don't run tests against actual CockroachDB instances, so these are unit tests asserting on SQL
        // only

        [Fact]
        public void CreateTableOperation_with_cockroach_interleave_in_parent()
        {
            var op =
                new CreateTableOperation
                {
                    Name = "People",
                    Schema = "dbo",
                    Columns =
                    {
                        new AddColumnOperation
                        {
                            Name = "Id",
                            Table = "People",
                            Schema = "dbo",
                            ClrType = typeof(int),
                            IsNullable = false
                        },
                    },
                    PrimaryKey = new AddPrimaryKeyOperation
                    {
                        Columns = new[] { "Id" }
                    }
                };

            var interleaveInParent = new CockroachDbInterleaveInParent(op);
            interleaveInParent.ParentTableSchema = "my_schema";
            interleaveInParent.ParentTableName = "my_parent";
            interleaveInParent.InterleavePrefix = new List<string> { "col_a", "col_b" };

            Generate(op);

            AssertSql(
                @"CREATE TABLE dbo.""People"" (
    ""Id"" integer NOT NULL,
    PRIMARY KEY (""Id"")
)
INTERLEAVE IN PARENT my_schema.my_parent (col_a, col_b);
");
        }

        #endregion CockroachDB interleave-in-parent

        #region Postgres-xl Distribute by

        [Fact]
        public void CreateTableOperation_with_postgresxl_distribute_by_replication()
        {
            var op =
                new CreateTableOperation
                {
                    Name = "People",
                    Schema = "dbo",
                    Columns =
                    {
                        new AddColumnOperation
                        {
                            Name = "Id",
                            Table = "People",
                            Schema = "dbo",
                            ClrType = typeof(int),
                            IsNullable = false
                        },
                    },
                    PrimaryKey = new AddPrimaryKeyOperation
                    {
                        Columns = new[] { "Id" }
                    }
                };

            var distribution = new PostgresXlDistributeBy(op);
            distribution.DistributionStrategy = PostgresXlDistributeByStrategy.Replication;

            Generate(op);

            AssertSql(
                @"CREATE TABLE dbo.""People"" (
    ""Id"" integer NOT NULL,
    PRIMARY KEY (""Id"")
)
DISTRIBUTE BY REPLICATION;
");
        }

        [Fact]
        public void CreateTableOperation_with_postgresxl_distribute_by_roundrobin()
        {
            var op =
                new CreateTableOperation
                {
                    Name = "People",
                    Schema = "dbo",
                    Columns =
                    {
                        new AddColumnOperation
                        {
                            Name = "Id",
                            Table = "People",
                            Schema = "dbo",
                            ClrType = typeof(int),
                            IsNullable = false
                        },
                    },
                    PrimaryKey = new AddPrimaryKeyOperation
                    {
                        Columns = new[] { "Id" }
                    }
                };

            var distribution = new PostgresXlDistributeBy(op);
            distribution.DistributionStrategy = PostgresXlDistributeByStrategy.RoundRobin;

            Generate(op);

            AssertSql(
                @"CREATE TABLE dbo.""People"" (
    ""Id"" integer NOT NULL,
    PRIMARY KEY (""Id"")
)
DISTRIBUTE BY ROUNDROBIN;
");
        }

        [Fact]
        public void CreateTableOperation_with_postgresxl_distribute_by_hash_column()
        {
            var op =
                new CreateTableOperation
                {
                    Name = "People",
                    Schema = "dbo",
                    Columns =
                    {
                        new AddColumnOperation
                        {
                            Name = "Id",
                            Table = "People",
                            Schema = "dbo",
                            ClrType = typeof(int),
                            IsNullable = false
                        },
                    },
                    PrimaryKey = new AddPrimaryKeyOperation
                    {
                        Columns = new[] { "Id" }
                    }
                };

            var distribution = new PostgresXlDistributeBy(op);
            distribution.DistributionStrategy = PostgresXlDistributeByStrategy.None;
            distribution.DistributeByColumnFunction = PostgresXlDistributeByColumnFunction.Hash;
            distribution.DistributeByPropertyName = "Id";

            Generate(op);

            AssertSql(
                @"CREATE TABLE dbo.""People"" (
    ""Id"" integer NOT NULL,
    PRIMARY KEY (""Id"")
)
DISTRIBUTE BY HASH (""Id"");
");
        }

        [Fact]
        public void CreateTableOperation_with_postgresxl_distribute_by_modulo_column()
        {
            var op =
                new CreateTableOperation
                {
                    Name = "People",
                    Schema = "dbo",
                    Columns =
                    {
                        new AddColumnOperation
                        {
                            Name = "Id",
                            Table = "People",
                            Schema = "dbo",
                            ClrType = typeof(int),
                            IsNullable = false
                        },
                    },
                    PrimaryKey = new AddPrimaryKeyOperation
                    {
                        Columns = new[] { "Id" }
                    }
                };

            var distribution = new PostgresXlDistributeBy(op);
            distribution.DistributionStrategy = PostgresXlDistributeByStrategy.None;
            distribution.DistributeByColumnFunction = PostgresXlDistributeByColumnFunction.Modulo;
            distribution.DistributeByPropertyName = "Id";

            Generate(op);

            AssertSql(
                @"CREATE TABLE dbo.""People"" (
    ""Id"" integer NOT NULL,
    PRIMARY KEY (""Id"")
)
DISTRIBUTE BY MODULO (""Id"");
");
        }

        [Fact]
        public void CreateTableOperation_with_postgresxl_distributed_by_column()
        {
            var op =
                new CreateTableOperation
                {
                    Name = "People",
                    Schema = "dbo",
                    Columns =
                    {
                        new AddColumnOperation
                        {
                            Name = "Id",
                            Table = "People",
                            Schema = "dbo",
                            ClrType = typeof(int),
                            IsNullable = false
                        },
                    },
                    PrimaryKey = new AddPrimaryKeyOperation
                    {
                        Columns = new[] { "Id" }
                    }
                };

            var distribution = new PostgresXlDistributeBy(op);
            distribution.DistributionStrategy = PostgresXlDistributeByStrategy.None;
            distribution.DistributeByPropertyName = "Id";

            Generate(op);

            AssertSql(
                @"CREATE TABLE dbo.""People"" (
    ""Id"" integer NOT NULL,
    PRIMARY KEY (""Id"")
)
DISTRIBUTED BY (""Id"");
");
        }

        [Fact]
        public void CreateTableOperation_with_postgresxl_distributed_randomly()
        {
            var op =
                new CreateTableOperation
                {
                    Name = "People",
                    Schema = "dbo",
                    Columns =
                    {
                        new AddColumnOperation
                        {
                            Name = "Id",
                            Table = "People",
                            Schema = "dbo",
                            ClrType = typeof(int),
                            IsNullable = false
                        },
                    },
                    PrimaryKey = new AddPrimaryKeyOperation
                    {
                        Columns = new[] { "Id" }
                    }
                };

            var distribution = new PostgresXlDistributeBy(op);
            distribution.DistributionStrategy = PostgresXlDistributeByStrategy.Randomly;

            Generate(op);

            AssertSql(
                @"CREATE TABLE dbo.""People"" (
    ""Id"" integer NOT NULL,
    PRIMARY KEY (""Id"")
)
DISTRIBUTED RANDOMLY;
");
        }

        [Fact]
        public void CreateTableOperation_with_postgresxl_diststyle_even()
        {
            var op =
                new CreateTableOperation
                {
                    Name = "People",
                    Schema = "dbo",
                    Columns =
                    {
                        new AddColumnOperation
                        {
                            Name = "Id",
                            Table = "People",
                            Schema = "dbo",
                            ClrType = typeof(int),
                            IsNullable = false
                        },
                    },
                    PrimaryKey = new AddPrimaryKeyOperation
                    {
                        Columns = new[] { "Id" }
                    }
                };

            var distribution = new PostgresXlDistributeBy(op);
            distribution.DistributionStyle = PostgresXlDistributionStyle.Even;

            Generate(op);

            AssertSql(
                @"CREATE TABLE dbo.""People"" (
    ""Id"" integer NOT NULL,
    PRIMARY KEY (""Id"")
)
DISTSTYLE EVEN;
");
        }
        [Fact]
        public void CreateTableOperation_with_postgresxl_diststyle_key()
        {
            var op =
                new CreateTableOperation
                {
                    Name = "People",
                    Schema = "dbo",
                    Columns =
                    {
                        new AddColumnOperation
                        {
                            Name = "Id",
                            Table = "People",
                            Schema = "dbo",
                            ClrType = typeof(int),
                            IsNullable = false
                        },
                    },
                    PrimaryKey = new AddPrimaryKeyOperation
                    {
                        Columns = new[] { "Id" }
                    }
                };

            var distribution = new PostgresXlDistributeBy(op);
            distribution.DistributionStyle = PostgresXlDistributionStyle.Key;
            distribution.DistributeByPropertyName = "Id";

            Generate(op);

            AssertSql(
                @"CREATE TABLE dbo.""People"" (
    ""Id"" integer NOT NULL,
    PRIMARY KEY (""Id"")
)
DISTSTYLE KEY DISTKEY (""Id"");
");
        }
        [Fact]
        public void CreateTableOperation_with_postgresxl_diststyle_all()
        {
            var op =
                new CreateTableOperation
                {
                    Name = "People",
                    Schema = "dbo",
                    Columns =
                    {
                        new AddColumnOperation
                        {
                            Name = "Id",
                            Table = "People",
                            Schema = "dbo",
                            ClrType = typeof(int),
                            IsNullable = false
                        },
                    },
                    PrimaryKey = new AddPrimaryKeyOperation
                    {
                        Columns = new[] { "Id" }
                    }
                };

            var distribution = new PostgresXlDistributeBy(op);
            distribution.DistributionStyle = PostgresXlDistributionStyle.All;

            Generate(op);

            AssertSql(
                @"CREATE TABLE dbo.""People"" (
    ""Id"" integer NOT NULL,
    PRIMARY KEY (""Id"")
)
DISTSTYLE ALL;
");
        }

        #endregion Postgres-xl Distribute by

#pragma warning disable 618
        [Fact]
        public virtual void AddColumnOperation_serial_old_annotation_throws()
        {
            Assert.Throws<NotSupportedException>(() =>
                Generate(new AddColumnOperation
                {
                    Table = "People",
                    Name = "foo",
                    ClrType = typeof(int),
                    ColumnType = "int",
                    IsNullable = false,
                    [NpgsqlAnnotationNames.ValueGeneratedOnAdd] = true
                }));
        }

        public override void InsertDataOperation_throws_for_unsupported_column_types()
            => Assert.Equal(
                RelationalStrings.UnsupportedDataOperationStoreType("foo", "dbo.People.First Name"),
                Assert.Throws<InvalidOperationException>(
                    () =>
                        Generate(
                            new InsertDataOperation
                            {
                                Table = "People",
                                Schema = "dbo",
                                Columns = new[] { "First Name" },
                                ColumnTypes = new[] { "foo" },
                                Values = new object[,] { { null } }
                            })).Message);

#pragma warning restore 618

        public NpgsqlMigrationsSqlGeneratorTest()
            : base(
                NpgsqlTestHelpers.Instance,
                new ServiceCollection().AddEntityFrameworkNpgsqlNetTopologySuite(),
                NpgsqlTestHelpers.Instance.AddProviderOptions(
                    ((IRelationalDbContextOptionsBuilderInfrastructure)
                        new NpgsqlDbContextOptionsBuilder(new DbContextOptionsBuilder()).UseNetTopologySuite())
                    .OptionsBuilder).Options)
        {
        }

        protected override string GetGeometryCollectionStoreType() => "GEOMETRY(GEOMETRYCOLLECTION)";
    }
}
