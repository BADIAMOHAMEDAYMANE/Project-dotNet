using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarRental.Core.Models
{
    [Table("Locations")]
    public class Location
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        [ForeignKey("Client")]
        public string ClientCIN { get; set; } = string.Empty;

        [Required]
        [ForeignKey("Vehicule")]
        public int VehiculeId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DateDebut { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DateFin { get; set; }

        [Required]
        [MaxLength(50)]
        public string Statut { get; set; } = "En attente";

        [Column(TypeName = "decimal(10,2)")]
        public decimal? PrixTotal { get; set; }

        [MaxLength(500)]
        public string? Commentaires { get; set; }

        // Navigation properties
        public virtual Client? Client { get; set; }
        public virtual Vehicule? Vehicule { get; set; }

        // Calculated properties (not mapped to database)
        [NotMapped]
        public int NombreJours => (DateFin - DateDebut).Days;

        [NotMapped]
        public bool EstActive => Statut == "En attente" || Statut == "Approuvée" || Statut == "Confirmée" || Statut == "En cours";

        [NotMapped]
        public bool EstEnCours => Statut == "En cours";
        public virtual Facture? Facture { get; set; }

        [NotMapped]
        public bool EstTerminee => Statut == "Terminée";

        // Method to calculate total amount - no attribute needed
        public decimal CalculerMontantTotal()
        {
            if (PrixTotal.HasValue)
                return PrixTotal.Value;

            if (Vehicule != null)
                return Vehicule.PrixParJour * NombreJours;

            return 0;
        }
    }
}