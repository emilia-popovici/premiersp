using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace PremierAuto.Models;

public class ApplicationUser : IdentityUser
{
    [StringLength(60)]
    public string? FirstName { get; set; }

    [StringLength(60)]
    public string? LastName { get; set; }
}
