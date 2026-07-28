namespace CliniKa.Application.DTOs.Patient
{
    public class AddMedicationRequest
    {
        public string DrugName { get; set; } = string.Empty;
        public string? Dosage { get; set; }
        public string? Frequency { get; set; }
    }
}
