using CliniKa.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace CliniKa.Application.Abstraction
{
    public interface IPatientService
    {
        Task<GeneralResult<PatientReadDTO>> GetPatientAsync(string query);
    }
}
