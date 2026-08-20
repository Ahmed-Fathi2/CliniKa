using System;

namespace CliniKa.Domain.Entities
{
    public class SessionRecommendedInvestigation
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public string Investigation { get; set; } = string.Empty;
        public int? Order { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual ClinicalSession ClinicalSession { get; set; } = null!;
    }
}
