using MedicalApi.Models;
using Microsoft.EntityFrameworkCore;

namespace MedicalApi.Data;

public class MedicalDbContext : DbContext
{
    public MedicalDbContext(DbContextOptions<MedicalDbContext> options) : base(options)
    {
    }

    public DbSet<Patient> Patients { get; set; }
    public DbSet<Spravka> Spravki { get; set; }
    public DbSet<SpravkaType> SpravkaTypes { get; set; }
    public DbSet<Appointment> Appointments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // длина текста и точность денег, как VARCHAR(100) и NUMERIC(10, 2) в задании 2
        modelBuilder.Entity<Patient>().Property(p => p.Name).HasMaxLength(100);
        modelBuilder.Entity<Patient>().Property(p => p.Phone).HasMaxLength(20);
        modelBuilder.Entity<SpravkaType>().Property(t => t.Name).HasMaxLength(100);
        modelBuilder.Entity<SpravkaType>().Property(t => t.Price).HasPrecision(10, 2);
        modelBuilder.Entity<Spravka>().Property(s => s.Name).HasMaxLength(100);
        modelBuilder.Entity<Spravka>().Property(s => s.Price).HasPrecision(10, 2);

        // Restrict: база не даст удалить пациента, на которого есть справки или приёмы,
        // и вид справки, по которому уже выписаны справки.
        // Справки и приёмы это медицинские записи, их нельзя удалять молча.

        // у справки один пациент, у пациента много справок, связь через PatientId
        modelBuilder.Entity<Spravka>()
            .HasOne<Patient>()
            .WithMany()
            .HasForeignKey(s => s.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        // у справки один вид, у вида много справок, связь через SpravkaTypeId
        modelBuilder.Entity<Spravka>()
            .HasOne<SpravkaType>()
            .WithMany()
            .HasForeignKey(s => s.SpravkaTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // у приёма один пациент, у пациента много приёмов
        modelBuilder.Entity<Appointment>()
            .HasOne<Patient>()
            .WithMany()
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
