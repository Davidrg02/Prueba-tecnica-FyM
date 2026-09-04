using FyM.Users.Application.Abstractions;
using FyM.Users.Application.Common;
using FyM.Users.Domain.Entities;
using FyM.Users.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FyM.Users.Infrastructure.Repositories;

public sealed class RefreshTokenRepository(AppDbContext db, IDateTimeProvider dateTimeProvider) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct) =>
        db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task RevokeAllActiveForUserAsync(Guid userId, string? revokedByIp, CancellationToken ct)
    {
        var now = dateTimeProvider.UtcNow;
        var activeTokens = await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null && t.ExpiresAtUtc > now)
            .ToListAsync(ct);

        foreach (var token in activeTokens)
        {
            token.RevokedAtUtc = now;
            token.RevokedByIp = revokedByIp;
        }
    }

    public void Add(RefreshToken token) => db.RefreshTokens.Add(token);
}
