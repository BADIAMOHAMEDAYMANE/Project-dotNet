using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarRental.Core.Models
{
    [Table("Vehicules")]
    public class Vehicule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public required string Marque { get; set; }

        [Required]
        [MaxLength(100)]
        public required string Modele { get; set; }

        public int? Annee { get; set; }

        [Required]
        [MaxLength(50)]
        public required string Immatriculation { get; set; }

        [MaxLength(50)]
        public string? Couleur { get; set; }

        public int NombrePlaces { get; set; } = 5;

        public int Kilometrage { get; set; } = 0;

        [ForeignKey("Categorie")]
        public int? CategorieId { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? PrixAchat { get; set; }

        // AJOUTEZ CETTE PROPRIÉTÉ :
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Le prix par jour doit être supérieur à 0")]
        [Display(Name = "Prix par jour")]
        public decimal PrixParJour { get; set; } = 50.00m;

        public DateTime? DateAchat { get; set; }

        [MaxLength(50)]
        public string Statut { get; set; } = "Disponible";

        public DateTime? DateDernierEntretien { get; set; }

        public int? KilometrageDernierEntretien { get; set; }

        public bool EstActif { get; set; } = true;

        public virtual CategorieVehicule? Categorie { get; set; }
        [NotMapped]
        public string MarqueModele => $"{Marque} {Modele}";
    }
}