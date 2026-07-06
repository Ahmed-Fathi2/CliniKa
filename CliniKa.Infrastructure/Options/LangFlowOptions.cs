using System.ComponentModel.DataAnnotations;

namespace CliniKa.Infrastructure.Options
{
    public class LangFlowOptions
    {
        public const string SectionName = "LangFlow";

        [Required]
        public string BaseUrl { get; set; } = string.Empty;

        [Required]
        public string ApiKey { get; set; } = string.Empty;

        [Required]
        public string AudioFlowId { get; set; } = string.Empty;

        [Required]
        public string VisionFlowId { get; set; } = string.Empty;

        [Required]
        public string SafetyFlowId { get; set; } = string.Empty;
    }
}
