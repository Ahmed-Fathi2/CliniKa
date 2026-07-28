namespace CliniKa.Application.DTOs.Patient
{
    public class AddAllergyRequest
    {
        /// <summary>
        /// Reference to an existing allergy in the catalog. Nullable if using RawAllergenName.
        /// </summary>
        public int? AllergyId { get; set; }

        /// <summary>
        /// Free-text allergen name typed by the patient. Nullable if using AllergyId.
        /// </summary>
        public string? RawAllergenName { get; set; }

        /// <summary>
        /// Severity level: "Mild", "Moderate", or "Severe".
        /// </summary>
        public string? Severity { get; set; }
    }
}
