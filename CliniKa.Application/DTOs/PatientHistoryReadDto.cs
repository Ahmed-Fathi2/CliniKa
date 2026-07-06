namespace CliniKa.Application.DTOs
{
    public class PatientHistoryReadDto
    {
        public int PatientId { get; set; }
        public string FullName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
        public List<ChronicConditionDto> ChronicConditions { get; set; } = new();
        public List<AllergyDto> Allergies { get; set; } = new();
        public List<MedicationDto> ActiveMedications { get; set; } = new();
    }
}
