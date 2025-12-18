using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarRental.Core.Models
{
    [Table("Factures")]
    public class Facture
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int LocationId { get; set; }

        [Required]
        public DateTime DateFacture { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(10,2)")]
        public decimal MontantTotal { get; set; }

        [MaxLength(20)]
        public string Format { get; set; } = "PDF"; // PDF ou CSV

        [MaxLength(300)]
        public string CheminFichier { get; set; } = string.Empty;

        // ✅ IMPORTANT: Ajouter ForeignKey attribute pour éviter LocationId1
        [ForeignKey("LocationId")]
        public virtual Location? Location { get; set; }
    }
}