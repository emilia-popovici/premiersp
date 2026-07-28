using System.ComponentModel.DataAnnotations;

namespace PremierAuto.ViewModels
{
    public class ChangeEmailViewModel
    {
        [Required(ErrorMessage = "Parola curentă este obligatorie.")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Noul email este obligatoriu.")]
        [EmailAddress(ErrorMessage = "Format de email invalid.")]
        public string NewEmail { get; set; }
    }
}