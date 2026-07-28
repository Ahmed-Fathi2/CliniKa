using CliniKa.Domain.Entities;
using CliniKa.Domain.Repositories;
using CliniKa.Infrastructure.Persistence.Context;

namespace CliniKa.Infrastructure.Repositories
{
    public class AllergyRepository : GenericRepository<Allergy, int>, IAllergyRepository
    {
        public AllergyRepository(AppDbContext context) : base(context) { }
    }
}
