using CliniKa.Domain.Entities;
using CliniKa.Domain.Repositories;
using CliniKa.Infrastructure.Persistence.Context;
using System;
using System.Collections.Generic;
using System.Text;

namespace CliniKa.Infrastructure.Repositories
{
    public class ActiveMedicationsRepository : GenericRepository<ActiveMedication, int>, IActiveMedicationsRepository
    {
        public ActiveMedicationsRepository(AppDbContext context) : base(context)
        {
            
        }
    }
}
