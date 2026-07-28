using System;
using System.Collections.Generic;

namespace CliniKa.Application.DTOs.Patient
{
    public class PatientMedicalDataDto
    {
        public int PatientId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? BloodType { get; set; }

        public List<MedicationDto> ActiveMedications { get; set; } = new();
        public List<DiseaseDto> Diseases { get; set; } = new();
        public List<AllergyDto> Allergies { get; set; } = new();
        public List<PatientSessionDto> Sessions { get; set; } = new();
    }
}
