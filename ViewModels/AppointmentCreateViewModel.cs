using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PremierAuto.ViewModels
{
    public class AppointmentCreateViewModel
    {
        [Required(ErrorMessage = "Te rugăm să selectezi un serviciu.")]
        public int ServiceId { get; set; }
        
        [Required(ErrorMessage = "Te rugăm să selectezi un mecanic.")]
        public int? MechanicId { get; set; }
        
        [Required(ErrorMessage = "Data și ora sunt obligatorii.")]
        public DateTime AppointmentDate { get; set; }

        [Required(ErrorMessage = "Marca mașinii este obligatorie.")]
        [StringLength(50)]
        public string CarMake { get; set; }

        [Required(ErrorMessage = "Modelul mașinii este obligatoriu.")]
        [StringLength(50)]
        public string CarModel { get; set; }

        [Required(ErrorMessage = "Te rugăm să adaugi detalii despre mașină sau problemă.")]
        public string Notes { get; set; }

        [ValidateNever]
        public IEnumerable<SelectListItem> Services { get; set; }
        
        [ValidateNever]
        public IEnumerable<SelectListItem> Mechanics { get; set; }
    }
}
