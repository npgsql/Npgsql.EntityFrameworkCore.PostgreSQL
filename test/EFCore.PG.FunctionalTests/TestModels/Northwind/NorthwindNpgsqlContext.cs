using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.TestModels.Northwind;

namespace Npgsql.EntityFrameworkCore.PostgreSQL.TestModels.Northwind
{
    public class NorthwindNpgsqlContext : NorthwindRelationalContext
    {
        public NorthwindNpgsqlContext(DbContextOptions options) : base(options) {}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasPostgresExtension("uuid-ossp");

            modelBuilder.Entity<Customer>()
                .Property(c => c.CustomerID)
                .HasColumnType("varchar(5)");

            modelBuilder.Entity<Employee>(
                b =>
                {
                    b.Property(c => c.EmployeeID).HasColumnType("int4");
                    b.Property(c => c.ReportsTo).HasColumnType("int4");
                });

            modelBuilder.Entity<Order>()
                .Property(o => o.EmployeeID)
                .HasColumnType("int4");

            modelBuilder.Entity<Product>().Property(p => p.UnitsInStock).HasColumnType("int2");

#pragma warning disable CS0618 // Type or member is obsolete
            modelBuilder.Query<CustomerView>().HasNoKey().ToQuery(
                () => CustomerQueries.FromSqlInterpolated($@"SELECT ""c"".""CustomerID"" || {_empty} as ""CustomerID"", ""c"".""Address"", ""c"".""City"", ""c"".""CompanyName"", ""c"".""ContactName"", ""c"".""ContactTitle"", ""c"".""Country"", ""c"".""Fax"", ""c"".""Phone"", ""c"".""PostalCode"", ""c"".""Region"" FROM ""Customers"" AS ""c"""
                ));
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}
