using Microsoft.EntityFrameworkCore;
using Minimal.API.Tutorial.Models;

namespace Minimal.API.Tutorial.Context
{
    public class MinimalDb : DbContext
    {
        public MinimalDb(DbContextOptions dbContextOptions) : base(dbContextOptions) { }
        public DbSet<MinimalItem> Items { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase("Items");
        }
    }
}
