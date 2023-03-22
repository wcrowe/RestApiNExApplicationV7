using Microsoft.EntityFrameworkCore;
using RestApiNLxV7.Data.Entities;

namespace RestApiNLxV7.Data.Context
{
    public class DataContext : DbContext
    {

        public DataContext(DbContextOptions<DataContext> options)
            : base(options)
        {
        }

        public DbSet<Account> Accounts { get; set; }

    }
}
