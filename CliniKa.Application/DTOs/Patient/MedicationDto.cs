namespace CliniKa.Application.DTOs.Patient
{
    public class MedicationDto
    {
        public string DrugName { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public int? SourceSessionId { get; set; }
        public string? PrescribedByDoctor { get; set; }
        public string? SourceSessionDate { get; set; }
    }
}
