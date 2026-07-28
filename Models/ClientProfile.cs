using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace PremierAuto.Models
{
    public class ClientProfile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }
        
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        [Required(ErrorMessage = "Prenumele este obligatoriu.")]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Numele de familie este obligatoriu.")]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Numărul de telefon este obligatoriu.")]
        [Phone(ErrorMessage = "Te rugăm să introduci un număr de telefon valid.")]
        public string PhoneNumber { get; set; }

        public string ProfilePictureUrl { get; set; } = string.Empty;
    }
}
