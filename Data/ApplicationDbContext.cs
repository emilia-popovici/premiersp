using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PremierAuto.Models;

namespace PremierAuto.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Service> Services { get; set; }
    public DbSet<Mechanic> Mechanics { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<ClientProfile> ClientProfiles { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<ContactMessage> ContactMessages { get; set; }
    public DbSet<AppointmentMessage> AppointmentMessages { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
{
    base.OnModelCreating(builder);
    builder.Entity<Mechanic>()
        .HasIndex(m => m.UserId)
        .IsUnique()
        .HasFilter("\"UserId\" IS NOT NULL");
    
    builder.Entity<Appointment>()
        .HasOne(a => a.Review)
        .WithOne(r => r.Appointment)
        .HasForeignKey<Review>(r => r.AppointmentId);
}
}
