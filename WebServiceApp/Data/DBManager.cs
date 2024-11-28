
using Microsoft.EntityFrameworkCore;
using DLL;

namespace WebServiceApp.Data
{
    public class DBManager : DbContext

    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(@"Data Source = Database.db;");
        }

        public DbSet<Client> Clients { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Client>();
        }
    }
}
