using CliniKa.Domain.Entities;
using CliniKa.Domain.Repositories;
using CliniKa.Infrastructure.Persistence.Context;

namespace CliniKa.Infrastructure.Repositories
{
    public class ChatSessionRepository : GenericRepository<ChatSession, int>, IChatSessionRepository
    {
        public ChatSessionRepository(AppDbContext context) : base(context)
        {
        }
    }
}
