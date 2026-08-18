using System;

namespace CliniKa.Application.DTOs.Sessions
{
    public class DoctorSessionReportDto
    {
        public int SessionId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string? PatientGender { get; set; }
        public string? PatientPhoneNumber { get; set; }
        public DateTime VisitDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? FinalDiagnosis { get; set; }
        public string? PossibleDiagnoses { get; set; }
        public string? PatientSummary { get; set; }
        public bool HasAiReport { get; set; }
        public int PrescribedMedicationsCount { get; set; }
    }
}
