namespace CliniKa.Application.DTOs.Allergy
{
    public class UpdateAllergyRequest
    {
        public string AllergenName { get; set; } = string.Empty;
        public string? AllergyType { get; set; }
    }
}
