using System.Collections.Generic;

namespace CliniKa.Application.DTOs.Patient
{
    public class AddMedicalInfoRequest
    {
        /// <summary>
        /// Basic medical data — "Male" or "Female".
        /// </summary>
        public string? Gender { get; set; }

        /// <summary>
        /// Blood type — e.g. "A+", "O-", "AB+".
        /// </summary>
        public string? BloodType { get; set; }

        /// <summary>
        /// National ID of the patient.
        /// </summary>
        public string? NationalId { get; set; }

        /// <summary>
        /// Date of birth of the patient.
        /// </summary>
        public DateTime? DateOfBirth { get; set; }

        /// <summary>
        /// Current medications the patient is taking.
        /// </summary>
        public IEnumerable<AddMedicationRequest>? Medications { get; set; }

        /// <summary>
        /// Known diseases — either select from catalog (DiseaseId) or type custom (RawDiseaseName).
        /// </summary>
        public IEnumerable<AddDiseaseRequest>? Diseases { get; set; }

        /// <summary>
        /// Known allergies — either select from catalog (AllergyId) or type custom (RawAllergenName).
        /// </summary>
        public IEnumerable<AddAllergyRequest>? Allergies { get; set; }
    }
}
