

namespace CliniKa.Domain.Entities
{
    public class Allergy
    {
        public int Id { get; set; }
        public string AllergenName { get; set; } = string.Empty;
        public string? AllergyType { get; set; }

        // Navigation properties
        public virtual ICollection<PatientAllergy> PatientAllergies { get; set; } = new List<PatientAllergy>();
    }
}
