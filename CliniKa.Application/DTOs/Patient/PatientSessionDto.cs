using System;

namespace CliniKa.Application.DTOs.Patient
{
    public class PatientSessionDto
    {
        public int SessionId { get; set; }
        public DateTime VisitDate { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? FinalDiagnosis { get; set; }
    }
}
