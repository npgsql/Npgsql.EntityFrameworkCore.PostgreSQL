using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.TestUtilities;
using Xunit;

namespace Npgsql.EntityFrameworkCore.PostgreSQL.Metadata
{
    public class NpgsqlBuilderExtensionsTest
    {
        [Fact]
        public void CockroachDbInterleaveInParent()
        {
            var modelBuilder = CreateConventionModelBuilder();

            modelBuilder.Entity<Customer>()
                .ToTable("customers", "my_schema")
                .UseCockroachDbInterleaveInParent(typeof(Customer), new List<string> { "col_a", "col_b" });

            var entityType = modelBuilder.Model.FindEntityType(typeof(Customer));
            var interleaveInParent = entityType.GetCockroachDbInterleaveInParent();

            Assert.Equal("my_schema", interleaveInParent.ParentTableSchema);
            Assert.Equal("customers", interleaveInParent.ParentTableName);
            var interleavePrefix = interleaveInParent.InterleavePrefix;
            Assert.Equal(2, interleavePrefix.Count);
            Assert.Equal("col_a", interleavePrefix[0]);
            Assert.Equal("col_b", interleavePrefix[1]);
        }

        [Fact]
        public void Can_set_identity_sequence_options_on_property()
        {
            var modelBuilder = CreateConventionModelBuilder();

            modelBuilder
                .Entity<Customer>()
                .Property(e => e.Id)
                .UseIdentityByDefaultColumn()
                .HasIdentityOptions(
                    startValue: 5,
                    incrementBy: 2,
                    minValue: 3,
                    maxValue: 2000,
                    cyclic: true,
                    numbersToCache: 10);

            var model = modelBuilder.Model;
            var property = model.FindEntityType(typeof(Customer)).FindProperty("Id");

            Assert.Equal(NpgsqlValueGenerationStrategy.IdentityByDefaultColumn, property.GetValueGenerationStrategy());
            Assert.Equal(ValueGenerated.OnAdd, property.ValueGenerated);
            Assert.Equal(5, property.GetIdentityStartValue());
            Assert.Equal(3, property.GetIdentityMinValue());
            Assert.Equal(2000, property.GetIdentityMaxValue());
            Assert.Equal(2, property.GetIdentityIncrementBy());
            Assert.True(property.GetIdentityIsCyclic());
            Assert.Equal(10, property.GetIdentityNumbersToCache());

            Assert.Null(model.FindSequence(NpgsqlModelExtensions.DefaultHiLoSequenceName));
            Assert.Null(model.FindSequence(NpgsqlModelExtensions.DefaultHiLoSequenceName));
        }

        [Fact]
        public void Is_partitioned_string_params()
        {
            var modelBuilder = CreateConventionModelBuilder();

            modelBuilder.Entity<Order>()
                .ToTable("orders", "my_schema")
                .IsPartitioned(TablePartitioningType.Range, "CustomerId", "Date");

            var entityType = modelBuilder.Model.FindEntityType(typeof(Order));

            var tablePartitioning = entityType.GetAnnotation(NpgsqlAnnotationNames.TablePartitioning)?.Value as TablePartitioning;
            Assert.NotNull(tablePartitioning);
            Assert.Equal(TablePartitioningType.Range, tablePartitioning.Type);
            Assert.Equal(2, tablePartitioning.PartitionKeyProperties.Length);
            Assert.Equal("CustomerId", tablePartitioning.PartitionKeyProperties[0].Name);
            Assert.Equal("Date", tablePartitioning.PartitionKeyProperties[1].Name);
        }

        [Fact]
        public void Is_partitioned_string_list()
        {
            var modelBuilder = CreateConventionModelBuilder();

            modelBuilder.Entity<Order>()
                .ToTable("orders", "my_schema")
                .IsPartitioned(TablePartitioningType.Range, new List<string> { "CustomerId", "Date" });

            var entityType = modelBuilder.Model.FindEntityType(typeof(Order));

            var tablePartitioning = entityType.GetAnnotation(NpgsqlAnnotationNames.TablePartitioning)?.Value as TablePartitioning;
            Assert.NotNull(tablePartitioning);
            Assert.Equal(TablePartitioningType.Range, tablePartitioning.Type);
            Assert.Equal(2, tablePartitioning.PartitionKeyProperties.Length);
            Assert.Equal("CustomerId", tablePartitioning.PartitionKeyProperties[0].Name);
            Assert.Equal("Date", tablePartitioning.PartitionKeyProperties[1].Name);
        }

        [Fact]
        public void Is_partitioned_properties_expression()
        {
            var modelBuilder = CreateConventionModelBuilder();

            modelBuilder.Entity<Order>()
                .ToTable("orders", "my_schema")
                .IsPartitioned(TablePartitioningType.Range, x => new { x.CustomerId, x.Date });

            var entityType = modelBuilder.Model.FindEntityType(typeof(Order));

            var tablePartitioning = entityType.GetAnnotation(NpgsqlAnnotationNames.TablePartitioning)?.Value as TablePartitioning;
            Assert.NotNull(tablePartitioning);
            Assert.Equal(TablePartitioningType.Range, tablePartitioning.Type);
            Assert.Equal(2, tablePartitioning.PartitionKeyProperties.Length);
            Assert.Equal("CustomerId", tablePartitioning.PartitionKeyProperties[0].Name);
            Assert.Equal("Date", tablePartitioning.PartitionKeyProperties[1].Name);
        }

        [Fact]
        public void Is_partitioned_property_expression()
        {
            var modelBuilder = CreateConventionModelBuilder();

            modelBuilder.Entity<Order>()
                .ToTable("orders", "my_schema")
                .IsPartitioned(TablePartitioningType.Range, x => x.Date);

            var entityType = modelBuilder.Model.FindEntityType(typeof(Order));

            var tablePartitioning = entityType.GetAnnotation(NpgsqlAnnotationNames.TablePartitioning)?.Value as TablePartitioning;
            Assert.NotNull(tablePartitioning);
            Assert.Equal(TablePartitioningType.Range, tablePartitioning.Type);
            Assert.Single(tablePartitioning.PartitionKeyProperties);
            Assert.Equal("Date", tablePartitioning.PartitionKeyProperties[0].Name);
        }

        [Fact]
        public void Is_partitioned_null_schema()
        {
            var modelBuilder = CreateConventionModelBuilder();

            modelBuilder.Entity<Order>()
                .ToTable("orders")
                .IsPartitioned(TablePartitioningType.Range, x => new { x.CustomerId, x.Date });

            var entityType = modelBuilder.Model.FindEntityType(typeof(Order));

            var tablePartitioning = entityType.GetAnnotation(NpgsqlAnnotationNames.TablePartitioning)?.Value as TablePartitioning;
            Assert.NotNull(tablePartitioning);
            Assert.Equal(TablePartitioningType.Range, tablePartitioning.Type);
            Assert.Equal(2, tablePartitioning.PartitionKeyProperties.Length);
            Assert.Equal("CustomerId", tablePartitioning.PartitionKeyProperties[0].Name);
            Assert.Equal("Date", tablePartitioning.PartitionKeyProperties[1].Name);
        }

        protected virtual ModelBuilder CreateConventionModelBuilder()
            => NpgsqlTestHelpers.Instance.CreateConventionBuilder();

        private class Customer
        {
            public int Id { get; set; }
            public string Name { get; set; }

            public IEnumerable<Order> Orders { get; set; }
        }

        private class Order
        {
            public int OrderId { get; set; }

            public int CustomerId { get; set; }

            public DateTime Date { get; set; }

            public Customer Customer { get; set; }

            public OrderDetails Details { get; set; }
        }

        private class OrderDetails
        {
            public int Id { get; set; }

            public int OrderId { get; set; }
            public Order Order { get; set; }
        }
    }
}
