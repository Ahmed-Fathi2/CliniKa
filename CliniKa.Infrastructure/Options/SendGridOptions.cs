using System.ComponentModel.DataAnnotations;

namespace CliniKa.Infrastructure.Options
{
    public class SendGridOptions
    {
        public const string SectionName = "SendGridSettings";

        [Required]
        public string ApiKey { get; set; } = string.Empty;

        [Required]
        public string SenderEmail { get; set; } = string.Empty;

        [Required]
        public string SenderName { get; set; } = string.Empty;
    }
}
