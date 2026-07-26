using System.Linq.Expressions;

namespace CliniKa.Domain.Entities
{
    public class ActiveMedication
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string DrugName { get; set; } = string.Empty;
        public string? Dosage { get; set; }
        public string? Frequency { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? SourceSessionId { get; set; }


        // Computed — no setter, no backing field
        public bool IsActive => EndDate == null || EndDate.Value.Date >= DateTime.UtcNow.Date;
        
        // Navigation properties
        public virtual Patient Patient { get; set; } = null!;
        public virtual ClinicalSession? SourceSession { get; set; }

        // Static filter expression for active medications
        public static Expression<Func<ActiveMedication, bool>> ActiveFilter =>
                    m => m.EndDate == null || m.EndDate.Value.Date >= DateTime.UtcNow.Date;
    }
}