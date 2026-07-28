using CliniKa.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CliniKa.Domain.Repositories
{
    public interface IClinicalSessionRepository : IGenericRepository<ClinicalSession, int>
    {
        Task<IEnumerable<ClinicalSession>> GetSessionsByPatientUserIdAsync(string userId);
        Task<ClinicalSession?> GetSessionDetailsByPatientUserIdAsync(int sessionId, string userId);
    }
}
