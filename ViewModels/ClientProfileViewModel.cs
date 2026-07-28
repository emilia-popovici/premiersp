using System.ComponentModel.DataAnnotations;

namespace PremierAuto.ViewModels
{
    public class ClientProfileViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Prenumele este obligatoriu.")]
        [StringLength(50)]
        public string? FirstName { get; set; }

        [Required(ErrorMessage = "Numele de familie este obligatoriu.")]
        [StringLength(50)]
        public string? LastName { get; set; }

        [Required(ErrorMessage = "Numărul de telefon este obligatoriu.")]
        [Phone(ErrorMessage = "Te rugăm să introduci un număr de telefon valid.")]
        public string? PhoneNumber { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }
}