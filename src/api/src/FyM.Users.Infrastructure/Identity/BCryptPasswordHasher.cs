using FyM.Users.Application.Abstractions;

namespace FyM.Users.Infrastructure.Identity;

public sealed class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Hash con formato inválido (p. ej. datos corruptos): se trata como no verificado, no como error 500.
            return false;
        }
    }
}
