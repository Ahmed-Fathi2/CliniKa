using CliniKa.Domain.Entities;
using CliniKa.Domain.Repositories;
using CliniKa.Infrastructure.Persistence.Context;

namespace CliniKa.Infrastructure.Repositories
{
    public class ClinicalSessionRepository : GenericRepository<ClinicalSession, int>, IClinicalSessionRepository
    {
        public ClinicalSessionRepository(AppDbContext context) : base(context)
        {
        }
    }
}
