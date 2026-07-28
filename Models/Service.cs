using System.ComponentModel.DataAnnotations;

namespace PremierAuto.Models
{
    public class Service
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        public int DurationMinutes { get; set; }

        public decimal Price { get; set; }
    }
}