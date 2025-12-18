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

        // DbSets
        public DbSet<Client> Clients { get; set; } = null!;
        public DbSet<Employee> Employees { get; set; } = null!;
        public DbSet<Vehicule> Vehicules { get; set; } = null!;
        public DbSet<CategorieVehicule> CategoriesVehicule { get; set; } = null!;
        public DbSet<Location> Locations { get; set; } = null!;
        public DbSet<Facture> Factures { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ========================================
            // CLIENT
            // ========================================
            modelBuilder.Entity<Client>(entity =>
            {
                entity.ToTable("Clients");
                entity.HasKey(e => e.CIN);

                entity.HasIndex(e => e.Email)
                    .IsUnique()
                    .HasDatabaseName("idx_client_email");

                entity.Property(e => e.CIN).HasMaxLength(20).IsRequired();
                entity.Property(e => e.Nom).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Prenom).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Telephone).HasMaxLength(20).IsRequired();
                entity.Property(e => e.NumeroPermis).HasMaxLength(50).IsRequired();

                entity.Property(e => e.DateInscription)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasMany(c => c.Locations)
                    .WithOne(l => l.Client)
                    .HasForeignKey(l => l.ClientCIN)
                    .HasPrincipalKey(c => c.CIN)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("fk_locations_clients");
            });

            // ========================================
            // EMPLOYEE
            // ========================================
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.ToTable("Employees");
                entity.HasKey(e => e.ID);

                entity.HasIndex(e => e.Email)
                    .IsUnique()
                    .HasDatabaseName("idx_employee_email");

                entity.Property(e => e.Nom).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Prenom).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(150).IsRequired();
                entity.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();

                entity.Property(e => e.Role)
                    .HasMaxLength(10)
                    .HasDefaultValue("employee");
            });

            // ========================================
            // CATEGORIE VEHICULE
            // ========================================
            modelBuilder.Entity<CategorieVehicule>(entity =>
            {
                entity.ToTable("CategoriesVehicule");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Nom).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
            });

            // ========================================
            // VEHICULE
            // ========================================
            modelBuilder.Entity<Vehicule>(entity =>
            {
                entity.ToTable("Vehicules");
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.Immatriculation)
                    .IsUnique()
                    .HasDatabaseName("idx_vehicule_immatriculation");

                entity.Property(e => e.Marque).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Modele).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Immatriculation).HasMaxLength(50).IsRequired();

                entity.Property(e => e.Statut)
                    .HasMaxLength(50)
                    .HasDefaultValue("Disponible");

                entity.Property(e => e.PrixParJour)
                    .HasColumnType("decimal(10,2)")
                    .HasDefaultValue(50.00m);

                entity.HasOne(v => v.Categorie)
                    .WithMany(c => c.Vehicules)
                    .HasForeignKey(v => v.CategorieId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("fk_vehicules_categories");
            });

            // ========================================
            // LOCATION
            // ========================================
            modelBuilder.Entity<Location>(entity =>
            {
                entity.ToTable("Locations");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.ClientCIN).HasMaxLength(20).IsRequired();
                entity.Property(e => e.DateDebut).HasColumnType("date").IsRequired();
                entity.Property(e => e.DateFin).HasColumnType("date").IsRequired();

                entity.Property(e => e.Statut)
                    .HasMaxLength(50)
                    .HasDefaultValue("En attente");

                entity.Property(e => e.PrixTotal)
                    .HasColumnType("decimal(10,2)")
                    .HasDefaultValue(0.00m);

                entity.HasOne(l => l.Client)
                    .WithMany(c => c.Locations)
                    .HasForeignKey(l => l.ClientCIN)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(l => l.Vehicule)
                    .WithMany()
                    .HasForeignKey(l => l.VehiculeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Ignore(l => l.NombreJours);
                entity.Ignore(l => l.EstActive);
                entity.Ignore(l => l.EstEnCours);
                entity.Ignore(l => l.EstTerminee);
            });

            // ========================================
            // FACTURE (RELATION 1–1 CORRIGÉE)
            // ========================================
            modelBuilder.Entity<Facture>(entity =>
            {
                entity.ToTable("Factures");
                entity.HasKey(f => f.Id);

                entity.Property(f => f.MontantTotal)
                    .HasColumnType("decimal(10,2)")
                    .IsRequired();

                entity.Property(f => f.Format)
                    .HasMaxLength(20)
                    .HasDefaultValue("PDF");

                entity.Property(f => f.CheminFichier)
                    .HasMaxLength(300)
                    .HasDefaultValue("");

                // ✅ ONE-TO-ONE PROPRE
                entity.HasOne(f => f.Location)
                    .WithOne(l => l.Facture)
                    .HasForeignKey<Facture>(f => f.LocationId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("fk_factures_locations");

                // Une seule facture par location
                entity.HasIndex(f => f.LocationId)
                    .IsUnique()
                    .HasDatabaseName("ux_facture_location");
            });

            // ========================================
            // SEED DATA
            // ========================================
            SeedEmployees(modelBuilder);
            SeedCategories(modelBuilder);
        }

        private void SeedEmployees(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Employee>().HasData(
                new Employee
                {
                    ID = 1,
                    Nom = "Admin",
                    Prenom = "System",
                    Email = "admin@carrental.com",
                    PasswordHash = "240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9",
                    Role = "admin"
                }
            );
        }

        private void SeedCategories(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CategorieVehicule>().HasData(
                new CategorieVehicule { Id = 1, Nom = "Économique" },
                new CategorieVehicule { Id = 2, Nom = "Berline" },
                new CategorieVehicule { Id = 3, Nom = "SUV" },
                new CategorieVehicule { Id = 4, Nom = "Luxe" },
                new CategorieVehicule { Id = 5, Nom = "Utilitaire" }
            );
        }
    }
}
