using Microsoft.EntityFrameworkCore.Storage;
using CliniKa.Domain.Repositories;
using CliniKa.Infrastructure.Persistence.Context;

namespace CliniKa.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IDbContextTransaction? _transaction;

        public IClinicalSessionRepository ClinicalSessions { get; }
        public IPatientRepository Patient { get; }
        public IDoctorRepository Doctors { get; }

        public IPrescribedMedicationsRepository PrescribedMedications { get; }

        public IActiveMedicationsRepository ActiveMedications { get; }

        public ISessionAIReportsRepository SessionAIReports { get; }
        public ISessionDifferentialDiagnosisRepository SessionDifferentialDiagnoses { get; }
        public ISessionRecommendedInvestigationRepository SessionRecommendedInvestigations { get; }

        public IChatSessionRepository ChatSessions { get; }

        public IDiseaseRepository Diseases { get; }

        public IAllergyRepository Allergies { get; }
        public IDashboardRepository Dashboard { get; }

        public UnitOfWork(
            AppDbContext context,
            IActiveMedicationsRepository activeMedications,
            IClinicalSessionRepository clinicalSessions, 
            IPatientRepository patient,
            IPrescribedMedicationsRepository prescribedMedications,
            IDoctorRepository doctors,
            ISessionAIReportsRepository sessionAIReports,
            ISessionDifferentialDiagnosisRepository sessionDifferentialDiagnoses,
            ISessionRecommendedInvestigationRepository sessionRecommendedInvestigations,
            IChatSessionRepository chatSessions,
            IDiseaseRepository diseases,
            IAllergyRepository allergies,
            IDashboardRepository dashboard)
        {
            _context = context;
            ActiveMedications = activeMedications;
            ClinicalSessions = clinicalSessions;
            Patient = patient;
            PrescribedMedications = prescribedMedications;
            Doctors = doctors;
            SessionAIReports = sessionAIReports;
            SessionDifferentialDiagnoses = sessionDifferentialDiagnoses;
            SessionRecommendedInvestigations = sessionRecommendedInvestigations;
            ChatSessions = chatSessions;
            Diseases = diseases;
            Allergies = allergies;
            Dashboard = dashboard;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction == null)
                throw new InvalidOperationException("No transaction started.");

            try
            {
                //await _context.SaveChangesAsync();
                await _transaction.CommitAsync();
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction == null)
                return;

            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        public async ValueTask DisposeAsync()
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
            }
            // context is usually disposed by the DI container if it's scoped, 
            // but we can call it here to be safe if this UoW is disposed manually.
            await _context.DisposeAsync();
        }
    }
}
