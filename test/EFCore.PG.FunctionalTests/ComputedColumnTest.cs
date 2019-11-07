using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL.TestUtilities;
using Npgsql.EntityFrameworkCore.PostgreSQL.TestUtilities.Xunit;
using Xunit;

namespace Npgsql.EntityFrameworkCore.PostgreSQL
{
    public class ComputedColumnTest : IDisposable
    {
        [MinimumPostgresVersionFact(12, 0)]
        public void Can_use_computed_columns()
        {
            var serviceProvider = new ServiceCollection()
                .AddEntityFrameworkNpgsql()
                .BuildServiceProvider();

            using var context = new Context(serviceProvider, TestStore.Name);
            context.Database.EnsureCreatedResiliently();

            var entity = context.Add(new Entity { P1 = 20, P2 = 30, P3 = 80 }).Entity;

            context.SaveChanges();

            Assert.Equal(50, entity.P4);
            Assert.Equal(100, entity.P5);
        }

        [MinimumPostgresVersionFact(12, 0)]
        public void Can_use_computed_columns_with_null_values()
        {
            var serviceProvider = new ServiceCollection()
                .AddEntityFrameworkNpgsql()
                .BuildServiceProvider();

            using var context = new Context(serviceProvider, TestStore.Name);
            context.Database.EnsureCreatedResiliently();

            var entity = context.Add(new Entity { P1 = 20, P2 = 30 }).Entity;

            context.SaveChanges();

            Assert.Equal(50, entity.P4);
            Assert.Null(entity.P5);
        }

        class Context : DbContext
        {
            readonly IServiceProvider _serviceProvider;
            readonly string _databaseName;

            public Context(IServiceProvider serviceProvider, string databaseName)
            {
                _serviceProvider = serviceProvider;
                _databaseName = databaseName;
            }

            public DbSet<Entity> Entities { get; set; }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
                => optionsBuilder
                    .UseNpgsql(NpgsqlTestStore.CreateConnectionString(_databaseName), b => b.ApplyConfiguration())
                    .UseInternalServiceProvider(_serviceProvider);

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<Entity>()
                    .Property(e => e.P4)
                    .HasComputedColumnSql(@"""P1"" + ""P2""");

                modelBuilder.Entity<Entity>()
                    .Property(e => e.P5)
                    .HasComputedColumnSql(@"""P1"" + ""P3""");
            }
        }

        class Entity
        {
            public int Id { get; set; }
            public int P1 { get; set; }
            public int P2 { get; set; }
            public int? P3 { get; set; }
            public int P4 { get; set; }
            public int? P5 { get; set; }
        }

        [Flags]
        public enum FlagEnum
        {
            None = 0x0,
            AValue = 0x1,
            BValue = 0x2
        }

        public class EnumItem
        {
            public int EnumItemId { get; set; }
            public FlagEnum FlagEnum { get; set; }
            public FlagEnum? OptionalFlagEnum { get; set; }
            public FlagEnum? CalculatedFlagEnum { get; set; }
        }

        class NullableContext : DbContext
        {
            readonly IServiceProvider _serviceProvider;
            readonly string _databaseName;

            public NullableContext(IServiceProvider serviceProvider, string databaseName)
            {
                _serviceProvider = serviceProvider;
                _databaseName = databaseName;
            }

            public DbSet<EnumItem> EnumItems { get; set; }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
                => optionsBuilder
                    .UseNpgsql(NpgsqlTestStore.CreateConnectionString(_databaseName), b => b.ApplyConfiguration())
                    .UseInternalServiceProvider(_serviceProvider);

            protected override void OnModelCreating(ModelBuilder modelBuilder)
                => modelBuilder.Entity<EnumItem>()
                    .Property(entity => entity.CalculatedFlagEnum)
                    .HasComputedColumnSql(@"""FlagEnum"" | ""OptionalFlagEnum""");
        }

        [MinimumPostgresVersionFact(12, 0)]
        public void Can_use_computed_columns_with_nullable_enum()
        {
            var serviceProvider = new ServiceCollection()
                .AddEntityFrameworkNpgsql()
                .BuildServiceProvider();

            using var context = new NullableContext(serviceProvider, TestStore.Name);
            context.Database.EnsureCreatedResiliently();

            var entity = context.EnumItems.Add(new EnumItem { FlagEnum = FlagEnum.AValue, OptionalFlagEnum = FlagEnum.BValue }).Entity;
            context.SaveChanges();

            Assert.Equal(FlagEnum.AValue | FlagEnum.BValue, entity.CalculatedFlagEnum);
        }

        public ComputedColumnTest()
        {
            TestStore = NpgsqlTestStore.CreateInitialized("ComputedColumnTest");
        }

        protected NpgsqlTestStore TestStore { get; }

        public virtual void Dispose() => TestStore.Dispose();
    }
}