using System.ComponentModel.DataAnnotations;

namespace CliniKa.Infrastructure.Options
{
    public class JwtOptions
    {
        public static string SectionName = "JwtSettings";

        [Required]
        public string SecretKey { get; set; } = string.Empty;
        
        [Required]
        public string Issuer { get; set; } = string.Empty;
        
        [Required]
        public string Audience { get; set; } = string.Empty;
        
        [Range(1, int.MaxValue)]
        public int ExpiryInDays { get; set; }
    }
}
