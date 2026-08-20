using System;
using System.Collections.Generic;

namespace CliniKa.Domain.Entities
{
    public class ClinicalSession
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public DateTime VisitDate { get; set; } = DateTime.UtcNow;
        public string? FinalDiagnosis { get; set; }
        public string? DoctorNotes { get; set; }
        public string? FollowUpRecommendations { get; set; }
        public string Status { get; set; } = "In Progress";
        public DateTime? FinalizedAt { get; set; }

        // Navigation properties
        public virtual Patient Patient { get; set; } = null!;
        public virtual Doctor Doctor { get; set; } = null!;
        public virtual SessionAiReport? SessionAiReport { get; set; }
        public virtual ICollection<UploadedDocument> UploadedDocuments { get; set; } = new List<UploadedDocument>();
        public virtual ICollection<PatientDisease> PatientDiseases { get; set; } = new List<PatientDisease>();
        public virtual ICollection<PatientAllergy> PatientAllergies { get; set; } = new List<PatientAllergy>();
        public virtual ICollection<ActiveMedication> ActiveMedications { get; set; } = new List<ActiveMedication>();
        public virtual ICollection<PrescribedMedication> PrescribedMedications { get; set; } = new List<PrescribedMedication>();
        public virtual ICollection<SessionFinding> SessionFindings { get; set; } = new List<SessionFinding>();
        public virtual ICollection<SessionDifferentialDiagnosis> DifferentialDiagnoses { get; set; } = new List<SessionDifferentialDiagnosis>();
        public virtual ICollection<SessionRecommendedInvestigation> RecommendedInvestigations { get; set; } = new List<SessionRecommendedInvestigation>();
    }
}
