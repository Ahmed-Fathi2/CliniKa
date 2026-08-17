using System;
using System.Collections.Generic;

namespace CliniKa.Domain.Entities
{
    public class Doctor
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Identity link
        public string UserId { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<ClinicalSession> ClinicalSessions { get; set; } = new List<ClinicalSession>();
    }
}
