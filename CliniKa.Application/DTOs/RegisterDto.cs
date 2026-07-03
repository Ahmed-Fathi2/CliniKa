namespace CliniKa.Application
{
    public record RegisterRequest(
        string FirstName,
        string LastName,
        string Email,
        string Password,
        string ConfirmPassword);

    public record RegisterResponse(
        string Id,
        string FirstName,
        string LastName,
        string Email
        );



}
