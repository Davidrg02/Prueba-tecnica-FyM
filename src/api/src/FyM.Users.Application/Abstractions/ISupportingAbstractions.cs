using FyM.Users.Domain.Entities;

namespace FyM.Users.Application.Abstractions;

/// <summary>Unidad de trabajo: agrupa los cambios de un caso de uso en una sola transacción de EF Core.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
}

/// <summary>Hashing de contraseñas. Implementado con BCrypt en Infrastructure.</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

/// <summary>Resultado de emitir un access token: el JWT firmado y su vigencia en segundos.</summary>
public readonly record struct AccessTokenResult(string Token, int ExpiresInSeconds);

/// <summary>
/// Emisión y hashing de tokens JWT y de refresh tokens. El servicio de
/// aplicación nunca ve la clave de firma ni el algoritmo: solo pide
/// "genera un access token para este usuario con estos roles/permisos".
/// </summary>
public interface ITokenService
{
    AccessTokenResult GenerateAccessToken(User user, IReadOnlyCollection<string> roles, IReadOnlyCollection<string> permissions, int maxRoleLevel);

    /// <summary>Genera un refresh token aleatorio en claro (el que recibe el cliente).</summary>
    string GenerateRefreshToken();

    /// <summary>Hash SHA-256 en Base64 de un refresh token, para persistir y comparar sin guardar el valor en claro.</summary>
    string HashRefreshToken(string rawToken);
}
