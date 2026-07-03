using System;

namespace CliniKa.Domain.Entities
{
    public class SessionFinding
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public string FindingType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Severity { get; set; }
        public string DoctorAction { get; set; } = "Pending";
        public DateTime LoggedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ClinicalSession ClinicalSession { get; set; } = null!;
    }
}
