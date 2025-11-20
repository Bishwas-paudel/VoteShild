using Microsoft.EntityFrameworkCore;
using VoteShield.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.General;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace VoteShield.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Report> Reports { get; set; }
        public DbSet<Candidate> Candidates { get; set; }
        public DbSet<ElectionEvent> ElectionEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Report>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.District);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.Status);

                entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.Location).IsRequired().HasMaxLength(200);
                entity.Property(e => e.District).IsRequired().HasMaxLength(50);
            });

            modelBuilder.Entity<ElectionEvent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.IsActive);
            });

            modelBuilder.Entity<Candidate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.District);
                entity.HasIndex(e => e.Party);
            });
        }
    }
}