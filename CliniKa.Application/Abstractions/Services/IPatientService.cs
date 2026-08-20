using CliniKa.Application.DTOs.Patient;

namespace CliniKa.Application.Abstraction
{
    public interface IPatientService
    {
        Task<GeneralResult<PatientReadDto>> GetPatientAsync(string query);
        Task<GeneralResult<PatientHistoryReadDto>> GetPatientHistoryAsync(int patientId);
        Task<GeneralResult> AddMedicalInfoAsync(AddMedicalInfoRequest request);
        Task<GeneralResult> UpdateMedicalInfoAsync(AddMedicalInfoRequest request);
        Task<GeneralResult<PatientProfileDto>> GetPatientProfileAsync();
        Task<GeneralResult<PatientPersonalInfoDto>> UpdatePatientPersonalInfoAsync(UpdatePatientPersonalInfoRequest request);
        Task<GeneralResult<PatientMedicalDataDto>> GetPatientMedicalDataAsync();
        Task<GeneralResult<IEnumerable<PatientSessionDto>>> GetPatientSessionsAsync();
        Task<GeneralResult<PatientSessionDetailsDto>> GetPatientSessionDetailsAsync(int sessionId);
    }
}
