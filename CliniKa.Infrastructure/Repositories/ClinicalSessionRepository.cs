using CliniKa.Domain.Entities;
using CliniKa.Domain.Repositories;
using CliniKa.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace CliniKa.Infrastructure.Repositories
{
    public class ClinicalSessionRepository : GenericRepository<ClinicalSession, int>, IClinicalSessionRepository
    {
        public ClinicalSessionRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<ClinicalSession>> GetSessionsByPatientUserIdAsync(string userId)
        {
            return await _context.Set<ClinicalSession>()
                .Include(cs => cs.Doctor)
                .Where(cs => cs.Patient.UserId == userId)
                .OrderByDescending(cs => cs.VisitDate)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<ClinicalSession?> GetSessionDetailsByPatientUserIdAsync(int sessionId, string userId)
        {
            return await _context.Set<ClinicalSession>()
                .Include(cs => cs.Doctor)
                .Include(cs => cs.Patient)
                .Include(cs => cs.PrescribedMedications)
                .Include(cs => cs.UploadedDocuments)
                .Include(cs => cs.SessionAiReport)
                .Where(cs => cs.Id == sessionId && cs.Patient.UserId == userId)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }
    }
}
