using CliniKa.Domain.Entities;
using CliniKa.Domain.Repositories;
using CliniKa.Infrastructure.Persistence.Context;

namespace CliniKa.Infrastructure.Repositories
{
    public class SessionAIReportsRepository : GenericRepository<SessionAiReport, int>, ISessionAIReportsRepository
    {
        public SessionAIReportsRepository(AppDbContext context): base(context)
        {
            
        }
    }
}
