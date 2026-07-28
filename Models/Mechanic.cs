using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace PremierAuto.Models
{
    public class Mechanic
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        public string? PhotoUrl { get; set; }

        [Range(0, 5)]
        public double Rating { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public bool IsPictureApproved { get; set; } = false;

        // Legătura cu contul de utilizator al mecanicului (rol "Mecanic").
        // Nullable: un mecanic poate exista în echipă fără cont încă legat.
        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }
    }
}
