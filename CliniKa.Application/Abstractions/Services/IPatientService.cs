using CliniKa.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace CliniKa.Application.Abstraction
{
    public interface IPatientService
    {
        Task<GeneralResult<PatientReadDto>> GetPatientAsync(string query);
        Task<GeneralResult<PatientHistoryReadDto>> GetPatientHistoryAsync(int patientId);
    }
}
