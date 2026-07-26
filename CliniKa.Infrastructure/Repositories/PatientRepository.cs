using CliniKa.Domain.Entities;
using CliniKa.Domain.Repositories;
using CliniKa.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace CliniKa.Infrastructure.Repositories
{
    public class PatientRepository : GenericRepository<Patient, int>, IPatientRepository
    {
        public PatientRepository(AppDbContext context) : base(context) { }

        public async Task<Patient?> GetPatientWithIncludesAsync(int patientId)
        {
            return await _context.Set<Patient>()
                .Include(p => p.PatientDiseases)
                    .ThenInclude(pd => pd.Disease)
                .Include(p => p.PatientAllergies)
                    .ThenInclude(pa => pa.Allergy)
                .Include(p => p.ActiveMedications)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == patientId);
        }
    }
}
