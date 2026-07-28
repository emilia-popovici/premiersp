using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace PremierAuto.Models
{
    public class Appointment
    {
        [Key]
        public int Id { get; set; }
        public int ServiceId { get; set; }
        [ForeignKey("ServiceId")]
        public Service Service { get; set; }

        public int? MechanicId { get; set; }
        [ForeignKey("MechanicId")]
        public Mechanic Mechanic { get; set; }

        public string ClientId { get; set; }
        [ForeignKey("ClientId")]
        public ApplicationUser Client { get; set; }

        public DateTime AppointmentDate { get; set; }
        public string Notes { get; set; }

        [Required(ErrorMessage = "Marca mașinii este obligatorie.")]
        [StringLength(50)]
        public string CarMake { get; set; }

        [Required(ErrorMessage = "Modelul mașinii este obligatoriu.")]
        [StringLength(50)]
        public string CarModel { get; set; }

        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        public Review Review { get; set; }
    }
}
