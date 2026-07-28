namespace CliniKa.Application.DTOs.Patient
{
    public class PrescribedMedicationDto
    {
        public int Id { get; set; }
        public string DrugName { get; set; } = string.Empty;
        public string? Dosage { get; set; }
        public string? Frequency { get; set; }
        public string? Duration { get; set; }
        public string? Notes { get; set; }
    }
}
