using CliniKa.Application.DTOs.Patient;

namespace CliniKa.Application.Abstraction
{
    public interface IPatientService
    {
        Task<GeneralResult<PatientReadDto>> GetPatientAsync(string query);
        Task<GeneralResult<PatientHistoryReadDto>> GetPatientHistoryAsync(int patientId);
    }
}
