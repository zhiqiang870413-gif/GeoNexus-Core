using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace GisProject.Models
{
   
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // 這代表資料庫裡會有一張叫 Vendors 的表
        public DbSet<Vendor> Vendors { get; set; }
    }
}
