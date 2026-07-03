namespace CliniKa.Application.Abstraction
{
    public interface IJwtProvider
    {
        (string token, int expiresIn) GenerateToken(string userId, string firstName, string lastName, string email, IEnumerable<string> roles);
    }
}
