using CliniKa.Domain.Entities;
using CliniKa.Domain.Repositories;
using CliniKa.Infrastructure.Persistence.Context;

namespace CliniKa.Infrastructure.Repositories
{
    public class SessionDifferentialDiagnosisRepository : GenericRepository<SessionDifferentialDiagnosis, int>, ISessionDifferentialDiagnosisRepository
    {
        public SessionDifferentialDiagnosisRepository(AppDbContext dbContext) : base(dbContext)
        {
        }
    }
}
