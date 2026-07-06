using CliniKa.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CliniKa.Domain.Repositories
{
    public interface IPatientRepository : IGenericRepository<Patient, int>
    {
    }
}
