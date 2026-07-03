using Microsoft.AspNetCore.Identity;

namespace CliniKa.Domain.Entities
{
    public class ApplicationRole : IdentityRole
    {
        public string? RoleDescription { get; set; }
    }
}
