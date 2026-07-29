namespace CliniKa.Application.DTOs.Disease
{
    public class UpdateDiseaseRequest
    {
        public string DiseaseCode { get; set; } = string.Empty;
        public string DiseaseName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
