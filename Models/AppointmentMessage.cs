using System;
using System.ComponentModel.DataAnnotations;

namespace PremierAuto.Models
{
    public class AppointmentMessage
    {
        public int Id { get; set; }
        
        // Legătura cu programarea
        public int AppointmentId { get; set; }
        public Appointment Appointment { get; set; }
        
        // Legătura cu cine a trimis mesajul (Client sau Admin)
        public string SenderId { get; set; }
        public ApplicationUser Sender { get; set; }

        // Să știm cum colorăm bulele de chat (ex: albastru pt admin, gri pt client)
        public bool IsAdmin { get; set; } 
        
        [Required]
        public string Text { get; set; }
        public bool IsRead { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Bucharest"));
    }
}