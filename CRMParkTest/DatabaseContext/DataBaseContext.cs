using Microsoft.EntityFrameworkCore;

namespace CRMParkTest.DatabaseContext
{
    internal class DataBaseContext : DbContext
    {
        public DbSet<LastValidCommand> Commands { get; set; }

        public DataBaseContext(DbContextOptions options) : base(options)
        {
            this.Database.EnsureCreated();
        }
    }
}
