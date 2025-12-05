using System;
using CarRental.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Client> Clients { get; set; } = null!;
        public DbSet<Employee> Employees { get; set; } = null!;

        // Ajoutez vos autres DbSet ici
        // public DbSet<Voiture> Voitures { get; set; } = null!;
        // public DbSet<Reservation> Reservations { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuration pour Client avec CIN comme clé primaire
            modelBuilder.Entity<Client>(entity =>
            {
                entity.HasKey(e => e.CIN);

                entity.HasIndex(e => e.Email).IsUnique();

                entity.Property(e => e.CIN).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Nom).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Prenom).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Telephone).IsRequired().HasMaxLength(20);
                entity.Property(e => e.NumeroPermis).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DateInscription).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Ignore("DateExpirationPermis");
            });

            // Configuration pour Employee
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasKey(e => e.ID);

                entity.HasIndex(e => e.Email).IsUnique();

                entity.Property(e => e.ID).ValueGeneratedOnAdd();
                entity.Property(e => e.Nom).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Prenom).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Role).IsRequired().HasMaxLength(10);
                entity.Property(e => e.DateCreation).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.DateModification).HasDefaultValueSql("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");
            });

            // Données de test pour Employee (compte admin par défaut)
            SeedEmployees(modelBuilder);
        }

        private void SeedEmployees(ModelBuilder modelBuilder)
        {
            // Hash du mot de passe "admin123" en SHA256
            string adminPasswordHash = "240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9";

            modelBuilder.Entity<Employee>().HasData(
                new Employee
                {
                    ID = 1,
                    Nom = "Admin",
                    Prenom = "System",
                    Email = "admin@carrental.com",
                    PasswordHash = adminPasswordHash,
                    Role = "admin",
                    DateCreation = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}