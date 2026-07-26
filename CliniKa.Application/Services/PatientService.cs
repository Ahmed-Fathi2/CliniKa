using CliniKa.Application.Abstraction;
using CliniKa.Application.DTOs;
using CliniKa.Domain.Repositories;

namespace CliniKa.Application.Services
{
    public class PatientService : IPatientService
    {
        private readonly IUnitOfWork _unitOfWork;
        public PatientService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<GeneralResult<PatientReadDto>> GetPatientAsync(string query)
        {
            var patient = await _unitOfWork.Patient.GetAsync(
                    filter: p => p.NationalId == query || p.FullName.Contains(query),
                    trackChanges: false
                );

            if (patient == null)
            {
                return GeneralResult<PatientReadDto>.NotFound("Patient not found.");
            }

            var patientReadDTO = new PatientReadDto
            {
                Id = patient.Id,
                FullName = patient.FullName,
                DateOfBirth = patient.DateOfBirth,
                Gender = patient.Gender,
                BloodType = patient.BloodType,
                NationalId = patient.NationalId,
                Phone = patient.Phone
            };

            return GeneralResult<PatientReadDto>.Success(patientReadDTO, "Patient retrieved successfully.");
        }

        public async Task<GeneralResult<PatientHistoryReadDto>> GetPatientHistoryAsync(int patientId)
        {
            var patient = await _unitOfWork.Patient.GetPatientWithIncludesAsync(patientId);

            if (patient == null)
            {
                return GeneralResult<PatientHistoryReadDto>.NotFound("Patient not found.");
            }

            var patientHistoryDto = new PatientHistoryReadDto
            {
                PatientId = patient.Id,
                FullName = patient.FullName,
                DateOfBirth = patient.DateOfBirth,

                ChronicConditions = patient.PatientDiseases.Select(pd => new ChronicConditionDto
                {
                    DiseaseCode = pd.Disease.DiseaseCode,
                    DiseaseName = pd.Disease.DiseaseName,
                    DiagnosedDate = pd.DiagnosedDate.ToString("yyyy-MM-dd")
                }).ToList(),

                Allergies = patient.PatientAllergies.Select(pa => new AllergyDto
                {
                    AllergenName = pa.Allergy.AllergenName,
                    Severity = pa.Severity
                }).ToList(),

                ActiveMedications = patient.ActiveMedications.Where(am => am.IsActive).Select(am => new MedicationDto
                {
                    DrugName = am.DrugName,
                    Dosage = am.Dosage ?? string.Empty,
                    Frequency = am.Frequency ?? string.Empty,
                    StartDate = am.StartDate.ToString("yyyy-MM-dd"),
                    EndDate = am.EndDate?.ToString("yyyy-MM-dd") ?? ""
                }).ToList()
            };

            return GeneralResult<PatientHistoryReadDto>.Success(patientHistoryDto, "Patient history retrieved successfully.");
        }
    }
}
