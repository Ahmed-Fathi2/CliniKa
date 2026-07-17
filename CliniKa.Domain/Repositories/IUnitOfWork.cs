using System;
using System.Threading.Tasks;

namespace CliniKa.Domain.Repositories
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        IClinicalSessionRepository ClinicalSessions { get; }
        IPatientRepository Patient { get; }
        IDoctorRepository Doctors { get; }

        Task<int> SaveChangesAsync();

        // Transaction methods
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
