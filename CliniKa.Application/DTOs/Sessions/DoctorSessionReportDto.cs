using System;

namespace CliniKa.Application.DTOs.Sessions
{
    public class DoctorSessionReportDto
    {
        public int SessionId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string? PatientGender { get; set; }
        public string? PatientNationalId { get; set; }
        public DateTime VisitDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? FinalDiagnosis { get; set; }
        public string? PossibleDiagnosis { get; set; }
        public string? HistoryOfPresentIllness { get; set; }
        public bool HasAiReport { get; set; }
        public int PrescribedMedicationsCount { get; set; }
    }
}
