namespace CliniKa.Application.DTOs.Allergy
{
    public class AllergyResponseDto
    {
        public int Id { get; set; }
        public string AllergenName { get; set; } = string.Empty;
        public string? AllergyType { get; set; }
    }
}
