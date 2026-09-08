using Microsoft.EntityFrameworkCore;
using TextileScout.Web.Models;

namespace TextileScout.Web.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Product> Products { get; set; }
    }
}