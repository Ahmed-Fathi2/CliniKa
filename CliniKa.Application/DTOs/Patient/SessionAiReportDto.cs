using System;
using System.Collections.Generic;

namespace CliniKa.Application.DTOs.Patient
{
    public class SessionAiReportDto
    {
        public int Id { get; set; }
        public string? Transcript { get; set; }
        public string? Analysis { get; set; }
        public string? ChiefComplaint { get; set; }
        public string? HistoryOfPresentIllness { get; set; }
        public string? CompleteFindingsFromAllMedicalImages { get; set; }
        public string? PossibleDiagnosis { get; set; }
        public List<string> RecommendedInvestigations { get; set; } = new();
        public string? ImmediateManagement { get; set; }
        public int? ImagesReceived { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}
