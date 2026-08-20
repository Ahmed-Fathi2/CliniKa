using System;
using System.Collections.Generic;

namespace CliniKa.Application.DTOs.Patient
{
    public class PatientSessionDetailsDto
    {
        public int SessionId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public int DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public DateTime VisitDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? FinalizedAt { get; set; }

        public string? FinalDiagnosis { get; set; }
        public string? DoctorNotes { get; set; }
        public string? FollowUpRecommendations { get; set; }

        public SessionAiReportDto? AiReport { get; set; }
        public List<PrescribedMedicationDto> PrescribedMedications { get; set; } = new();
        public List<UploadedDocumentDto> UploadedDocuments { get; set; } = new();
    }
}
