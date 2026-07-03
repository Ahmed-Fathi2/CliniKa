namespace CliniKa.Application.DTOs
{
    public record ResetPasswordRequest(string Email, string Code, string NewPassword);
}
