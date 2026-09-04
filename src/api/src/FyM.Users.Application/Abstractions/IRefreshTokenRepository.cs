using FyM.Users.Domain.Entities;

namespace FyM.Users.Application.Abstractions;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct);

    /// <summary>Revoca todos los tokens activos de un usuario. Se usa en logout,
    /// cambio de contraseña, desactivación de cuenta y como respuesta ante la
    /// detección de reuso de un token ya rotado (posible robo de sesión).</summary>
    Task RevokeAllActiveForUserAsync(Guid userId, string? revokedByIp, CancellationToken ct);

    void Add(RefreshToken token);
}
