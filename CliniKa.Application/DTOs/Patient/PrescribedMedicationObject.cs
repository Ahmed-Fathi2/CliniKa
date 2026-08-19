namespace CliniKa.Application.DTOs.Patient
{
    public class PrescribedMedicationObject
    {
        public string DrugName { get; set; }
        public string Dosage { get; set; }
        public string Frequency { get; set; }
        public string Duration { get; set; }
        public string Notes { get; set; }
        public string DoctorName { get; set; }
        public string SessionDate { get; set; }
    }
}
