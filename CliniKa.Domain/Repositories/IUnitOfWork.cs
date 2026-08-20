using System;
using System.Threading.Tasks;

namespace CliniKa.Domain.Repositories
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        IClinicalSessionRepository ClinicalSessions { get; }
        IPatientRepository Patient { get; }
        IDoctorRepository Doctors { get; }
        IPrescribedMedicationsRepository PrescribedMedications { get; }
        IActiveMedicationsRepository ActiveMedications { get; }
        ISessionAIReportsRepository SessionAIReports { get; }
        ISessionDifferentialDiagnosisRepository SessionDifferentialDiagnoses { get; }
        ISessionRecommendedInvestigationRepository SessionRecommendedInvestigations { get; }
        IChatSessionRepository ChatSessions { get; }
        IDiseaseRepository Diseases { get; }
        IAllergyRepository Allergies { get; }
        IDashboardRepository Dashboard { get; }
        Task<int> SaveChangesAsync();

        // Transaction methods
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
