using CliniKa.Domain.Entities;
using CliniKa.Domain.Repositories;
using CliniKa.Infrastructure.Persistence.Context;

namespace CliniKa.Infrastructure.Repositories
{
    public class SessionRecommendedInvestigationRepository : GenericRepository<SessionRecommendedInvestigation, int>, ISessionRecommendedInvestigationRepository
    {
        public SessionRecommendedInvestigationRepository(AppDbContext context) : base(context)
        {
        }
    }
}
