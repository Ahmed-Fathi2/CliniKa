namespace CliniKa.Application.DTOs.Authentication
{
    public record RegisterRequest(
        string FirstName,
        string LastName,
        string PhoneNumber,
        string Email,
        string Password,
        string ConfirmPassword);

    public record RegisterResponse(
        string Id,
        string FirstName,
        string LastName,
        string PhoneNumber,
        string Email
        );



}
