namespace CliniKa.Application.Abstractions.Authentication
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
        string? UserName { get; }
        bool IsAuthenticated { get; }
        bool IsAdmin { get; }
        bool IsDoctor { get; }
        bool IsPatient { get; }
    }
}
