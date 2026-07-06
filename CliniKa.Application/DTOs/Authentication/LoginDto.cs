namespace CliniKa.Application.DTOs.Authentication
{
    public record LoginRequest(string Email, string Password);
    public record LoginResponse(
        string Id, 
        string FirstName, 
        string LastName, 
        string Email, 
        string Token, 
        int ExpiresIn);
}
