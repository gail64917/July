using DebtCardService.Models.DebtCard;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DebtCardService.Data
{
    public class DebtCardContext : DbContext
    {
        public DebtCardContext(DbContextOptions<DebtCardContext> options) : base(options)
        {
        }

        public DbSet<DebtCard> DebtCards { get; set; }
        public DbSet<LibrarySystem> LibrarySystems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DebtCard>().ToTable("DebtCard");
            modelBuilder.Entity<LibrarySystem>().ToTable("LibrarySystem");
        }
    }
}
