using System.ComponentModel.DataAnnotations;

namespace PremierAuto.Models
{
    public class ClientCar
    {
        public int Id { get; set; }

        [Required]
        public string ClientId { get; set; } = string.Empty;
        public ApplicationUser? Client { get; set; }

        [Required(ErrorMessage = "Marca este obligatorie.")]
        [StringLength(50)]
        public string CarMake { get; set; } = string.Empty;

        [Required(ErrorMessage = "Modelul este obligatoriu.")]
        [StringLength(50)]
        public string CarModel { get; set; } = string.Empty;

        [StringLength(15, ErrorMessage = "Numărul de înmatriculare este prea lung.")]
        public string? LicensePlate { get; set; } 
        public string? VIN { get; set; }
        public int? Year { get; set; }
    }
}