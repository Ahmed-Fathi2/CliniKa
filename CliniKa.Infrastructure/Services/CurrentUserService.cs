using CliniKa.Application.Abstractions.Authentication;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace CliniKa.Infrastructure.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
            => _httpContextAccessor = httpContextAccessor;

        public string? UserId
        {
            get
            {
                return _httpContextAccessor.HttpContext?
                    .User.FindFirstValue(ClaimTypes.NameIdentifier);
            }
        }

        public string? UserName =>
            _httpContextAccessor.HttpContext?.User
                .FindFirstValue(ClaimTypes.Name);

        public bool IsAuthenticated =>
            _httpContextAccessor.HttpContext?.User
                .Identity?.IsAuthenticated ?? false;

        public bool IsAdmin =>
            _httpContextAccessor.HttpContext?.User
                .IsInRole("Admin") ?? false;
        public bool IsDoctor =>
            _httpContextAccessor.HttpContext?.User
                .IsInRole("Doctor") ?? false;
        public bool IsPatient =>
            _httpContextAccessor.HttpContext?.User
                .IsInRole("Patient") ?? false;
    }
}
