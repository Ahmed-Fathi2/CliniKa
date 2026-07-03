using System;
using System.Collections.Generic;

namespace CliniKa.Domain.Entities
{
    public class Patient
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? BloodType { get; set; }
        public string? NationalId { get; set; }
        public string? Phone { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Identity link
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = null!;

        // Navigation properties
        public virtual ICollection<ClinicalSession> ClinicalSessions { get; set; } = new List<ClinicalSession>();
        public virtual ICollection<PatientDisease> PatientDiseases { get; set; } = new List<PatientDisease>();
        public virtual ICollection<PatientAllergy> PatientAllergies { get; set; } = new List<PatientAllergy>();
        public virtual ICollection<ActiveMedication> ActiveMedications { get; set; } = new List<ActiveMedication>();
    }
}
