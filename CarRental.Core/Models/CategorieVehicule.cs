using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarRental.Core.Models
{
    [Table("CategoriesVehicule")]
    public class CategorieVehicule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public required string Nom { get; set; }

        public string? Description { get; set; }

        // Navigation property
        public virtual ICollection<Vehicule>? Vehicules { get; set; }
    }
}