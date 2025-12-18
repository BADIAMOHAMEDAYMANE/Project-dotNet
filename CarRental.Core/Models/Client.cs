using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarRental.Core.Models
{
    [Table("Clients")]
    public class Client
    {
        [Key]
        [MaxLength(20)]
        public string CIN { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Nom { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Prenom { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Telephone { get; set; } = string.Empty;

        public string Adresse { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Ville { get; set; } = string.Empty;

        [MaxLength(10)]
        public string CodePostal { get; set; } = string.Empty;

        [MaxLength(50)]
        public string NumeroPermis { get; set; } = string.Empty;

        [Required]
        public DateTime DateNaissance { get; set; }

        public DateTime DateInscription { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(255)]
        public string Password { get; set; } = string.Empty;

        // Champ de confirmation non stocké en BD
        [NotMapped]
        [Compare("Password", ErrorMessage = "Les mots de passe ne correspondent pas")]
        public string? ConfirmPassword { get; set; }

        // ✅ CORRECTION : Retirer [NotMapped] pour permettre la navigation EF Core
        // Navigation property - EF Core gère automatiquement la relation
        public virtual ICollection<Location>? Locations { get; set; }
    }
}