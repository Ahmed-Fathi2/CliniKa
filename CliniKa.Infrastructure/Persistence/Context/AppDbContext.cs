using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CliniKa.Domain.Entities;
using CliniKa.Infrastructure.Persistence.Seeders;
using CliniKa.Infrastructure.Persistence.Identity;

namespace CliniKa.Infrastructure.Persistence.Context
{
    public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Clinical entities
        public DbSet<Doctor> Doctors => Set<Doctor>();
        public DbSet<Patient> Patients => Set<Patient>();
        public DbSet<ClinicalSession> ClinicalSessions => Set<ClinicalSession>();
        public DbSet<SessionAiReport> SessionAIReports => Set<SessionAiReport>();
        public DbSet<UploadedDocument> UploadedDocuments => Set<UploadedDocument>();
        public DbSet<Disease> Diseases => Set<Disease>();
        public DbSet<PatientDisease> PatientDiseases => Set<PatientDisease>();
        public DbSet<Allergy> Allergies => Set<Allergy>();
        public DbSet<PatientAllergy> PatientAllergies => Set<PatientAllergy>();
        public DbSet<ActiveMedication> ActiveMedications => Set<ActiveMedication>();
        public DbSet<PrescribedMedication> PrescribedMedications => Set<PrescribedMedication>();
        public DbSet<SessionFinding> SessionFindings => Set<SessionFinding>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            builder.SeedRolesAndUser();
        }
    }
}
