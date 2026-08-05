using System;
using System.ComponentModel.DataAnnotations;

namespace PremierAuto.Models
{
    public class JobPosition
    {
        public int Id { get; set; }
        
        [Required]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        public bool IsHiring { get; set; } = true;
    }

    public enum ApplicationStatus
    {
        Nou,
        Contactat,
        Respins
    }

    public class JobApplication
    {
        public int Id { get; set; }
        
        public int JobPositionId { get; set; }
        public JobPosition? JobPosition { get; set; }

        [Required]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        public string LastName { get; set; } = string.Empty;
        
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string Phone { get; set; } = string.Empty;

        public string? CvUrl { get; set; }

        public string? Education { get; set; }
        public string? Experience { get; set; }
        
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Nou;
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}