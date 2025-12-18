using System.ComponentModel.DataAnnotations;

namespace CarRental.Core.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Le nom est requis")]
        [StringLength(50, ErrorMessage = "Le nom ne peut pas dépasser 50 caractères")]
        [Display(Name = "Nom")]
        public required string Nom { get; set; }

        [Required(ErrorMessage = "Le prénom est requis")]
        [StringLength(50, ErrorMessage = "Le prénom ne peut pas dépasser 50 caractères")]
        [Display(Name = "Prénom")]
        public required string Prenom { get; set; }

        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [StringLength(100, ErrorMessage = "L'email ne peut pas dépasser 100 caractères")]
        [Display(Name = "Adresse email")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Le mot de passe est requis")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Le mot de passe doit contenir au moins 6 caractères")]
        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe")]
        public required string Password { get; set; }

        [Required(ErrorMessage = "La confirmation du mot de passe est requise")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmer le mot de passe")]
        [Compare("Password", ErrorMessage = "Les mots de passe ne correspondent pas")]
        public required string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Le rôle est requis")]
        [Display(Name = "Rôle")]
        public required string Role { get; set; } = "employee"; // Valeur par défaut
    }
}