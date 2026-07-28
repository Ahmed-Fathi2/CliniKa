using CliniKa.Domain.Entities;
using CliniKa.Domain.Repositories;
using CliniKa.Infrastructure.Persistence.Context;

namespace CliniKa.Infrastructure.Repositories
{
    public class DiseaseRepository : GenericRepository<Disease, int>, IDiseaseRepository
    {
        public DiseaseRepository(AppDbContext context) : base(context) { }
    }
}
