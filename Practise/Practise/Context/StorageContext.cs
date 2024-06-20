using Microsoft.EntityFrameworkCore;
using Storage.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storage.Core.Context
{
    public class StorageContext:DbContext
    {

        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<StorageTransaction> StorageTransactions  => Set<StorageTransaction>();
        public DbSet<TransactionType> TransactionTypes => Set<TransactionType>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderDetails> OrderDetails => Set<OrderDetails>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Status> Statuses => Set<Status>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = "Server=.;Database=StorageDB;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";
            optionsBuilder.UseSqlServer(connectionString);

            base.OnConfiguring(optionsBuilder);
        }
    }
}
