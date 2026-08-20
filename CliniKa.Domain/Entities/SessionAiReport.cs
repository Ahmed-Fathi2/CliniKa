using System;

namespace CliniKa.Domain.Entities
{
    public class SessionAiReport
    {
        public int Id { get; set; }
        public int SessionId { get; set; }

        public string? Transcript { get; set; }
        public string? Analysis { get; set; }
        public string? ChiefComplaint { get; set; }
        public string? HistoryOfPresentIllness { get; set; }
        public string? CompleteFindingsFromAllMedicalImages { get; set; }
        public string? PossibleDiagnosis { get; set; }
        public string? ImmediateManagement { get; set; }
        public int? ImagesReceived { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ClinicalSession ClinicalSession { get; set; } = null!;
    }
}
