using System;
using System.ComponentModel.DataAnnotations;

namespace PremierAuto.Models
{
    public class ContactMessage
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Numele este obligatoriu.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Telefonul este obligatoriu.")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Mesajul este obligatoriu.")]
        public string Message { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;
    }
}