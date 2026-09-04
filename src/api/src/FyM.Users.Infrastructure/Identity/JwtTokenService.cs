using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FyM.Users.Application.Abstractions;
using FyM.Users.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace FyM.Users.Infrastructure.Identity;

/// <summary>
/// Emite access tokens JWT (HS256) y refresh tokens aleatorios. Los roles y
/// permisos del usuario viajan como claims dentro del propio token para que
/// la autorización no necesite volver a consultar la base de datos en cada
/// request; <c>sstamp</c> permite invalidarlos antes de su expiración natural
/// (ver <c>JwtBearerEvents.OnTokenValidated</c> en la capa Api).
/// </summary>
public sealed class JwtTokenService(JwtOptions options) : ITokenService
{
    public AccessTokenResult GenerateAccessToken(
        User user, IReadOnlyCollection<string> roles, IReadOnlyCollection<string> permissions, int maxRoleLevel)
    {
        var expiresInSeconds = options.AccessTokenMinutes * 60;
        var now = DateTime.UtcNow;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("name", user.UserName),
            new("sstamp", user.SecurityStamp.ToString()),
            new("rlevel", maxRoleLevel.ToString()),
        };
        claims.AddRange(roles.Select(r => new Claim("role", r)));
        claims.AddRange(permissions.Select(p => new Claim("permission", p)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddSeconds(expiresInSeconds),
            signingCredentials: credentials);

        return new AccessTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresInSeconds);
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    public string HashRefreshToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToBase64String(bytes);
    }
}
