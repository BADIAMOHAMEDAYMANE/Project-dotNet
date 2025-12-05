using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CarRental.Core.Models
{
    public class Client
    {
        [Key]
        [MaxLength(20)]
        public required string CIN { get; set; }

        [MaxLength(100)]
        public required string Nom { get; set; }

        [MaxLength(100)]
        public required string Prenom { get; set; }

        public DateTime DateNaissance { get; set; }

        [MaxLength(100)]
        public required string Email { get; set; }

        [MaxLength(20)]
        public required string Telephone { get; set; }

        public required string Adresse { get; set; }

        [MaxLength(100)]
        public required string Ville { get; set; }

        [MaxLength(10)]
        public required string CodePostal { get; set; }

        [MaxLength(50)]
        public required string NumeroPermis { get; set; }

        public DateTime DateInscription { get; set; } = DateTime.Now;

        
     
    }
}