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
        public int? Year { get; set; }
        public string? VehicleType { get; set; }
        public string? VIN { get; set; }
        public string? BodyNumber { get; set; }
        public string? ChassisNumber { get; set; }
        public int? MaxMass { get; set; }
        public int? OwnMass { get; set; }
        public string? Category { get; set; }
        public string? BodyStyle { get; set; }
        public int? EngineCapacity { get; set; }
        public string? FuelType { get; set; }
        public string? PowerWeightRatio { get; set; }
        public string? Color { get; set; }
        public int? Seats { get; set; }
        public string? IDNV { get; set; }
    }
}