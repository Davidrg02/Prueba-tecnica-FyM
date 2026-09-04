using FyM.Users.Application.Abstractions;
using FyM.Users.Application.Common;
using FyM.Users.Domain.Entities;
using FyM.Users.Domain.Enums;
using FyM.Users.Domain.Exceptions;

namespace FyM.Users.Application.Auth;

/// <summary>
/// Orquesta registro, login, rotación de refresh tokens y cambio de
/// contraseña. La rotación de refresh tokens (<see cref="RefreshAsync"/>)
/// implementa detección de reuso: si llega un token ya revocado, se asume
/// robo de sesión y se revoca toda la familia de tokens del usuario.
/// </summary>
public sealed class AuthService(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IDateTimeProvider dateTimeProvider,
    AuthOptions authOptions) : IAuthService
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    public async Task<LoginResult> RegisterAsync(RegisterRequest request, string? ipAddress, string? userAgent, CancellationToken ct)
    {
        if (!authOptions.AllowSelfRegistration)
        {
            throw new ForbiddenOperationException("El auto-registro está deshabilitado.");
        }

        var normalizedEmail = Normalize(request.Email);
        if (await userRepository.ExistsByEmailAsync(normalizedEmail, ct))
        {
            throw new ConflictException("Ya existe un usuario registrado con ese correo electrónico.");
        }

        if (await userRepository.ExistsByUserNameAsync(request.UserName, ct))
        {
            throw new ConflictException("Ya existe un usuario con ese nombre de usuario.");
        }

        var userRole = await roleRepository.GetByNameAsync(Normalize(SystemRole.User), ct)
            ?? throw new InvalidOperationException("El rol 'User' no está sembrado en la base de datos.");

        var now = dateTimeProvider.UtcNow;
        var user = new User
        {
            UserName = request.UserName,
            Email = request.Email,
            NormalizedEmail = normalizedEmail,
            PasswordHash = passwordHasher.Hash(request.Password),
            Profile = new UserProfile
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                DocumentTypeId = request.DocumentTypeId,
                DocumentNumber = request.DocumentNumber,
                PhoneNumber = request.PhoneNumber,
            },
        };
        user.UserRoles.Add(new UserRole { Role = userRole, AssignedAtUtc = now });

        userRepository.Add(user);

        return await IssueTokensAsync(user, [userRole], ipAddress, userAgent, tokenToRotate: null, ct);
    }

    public async Task<LoginResult> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent, CancellationToken ct)
    {
        var user = await userRepository.GetByEmailAsync(Normalize(request.Email), ct)
            ?? throw new BusinessRuleException("Las credenciales ingresadas son inválidas.");

        var now = dateTimeProvider.UtcNow;

        if (user.IsLockedOut(now))
        {
            throw new ForbiddenOperationException("La cuenta está bloqueada temporalmente por múltiples intentos fallidos. Intente de nuevo más tarde.");
        }

        if (!user.IsActive)
        {
            throw new ForbiddenOperationException("La cuenta está desactivada.");
        }

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.AccessFailedCount++;
            if (user.AccessFailedCount >= MaxFailedAttempts)
            {
                user.LockoutEndUtc = now.Add(LockoutDuration);
                user.AccessFailedCount = 0;
            }

            await unitOfWork.SaveChangesAsync(ct);
            throw new BusinessRuleException("Las credenciales ingresadas son inválidas.");
        }

        user.AccessFailedCount = 0;
        user.LockoutEndUtc = null;
        user.LastLoginUtc = now;

        var roles = user.UserRoles.Select(ur => ur.Role).ToList();
        return await IssueTokensAsync(user, roles, ipAddress, userAgent, tokenToRotate: null, ct);
    }

    public async Task<LoginResult> RefreshAsync(string refreshToken, string? ipAddress, string? userAgent, CancellationToken ct)
    {
        var hash = tokenService.HashRefreshToken(refreshToken);
        var stored = await refreshTokenRepository.GetByTokenHashAsync(hash, ct)
            ?? throw new ForbiddenOperationException("El refresh token no es válido.");

        var now = dateTimeProvider.UtcNow;

        if (stored.RevokedAtUtc is not null)
        {
            // Un token ya rotado que vuelve a usarse indica posible robo de la
            // cookie: se responde revocando toda la familia de sesiones.
            await refreshTokenRepository.RevokeAllActiveForUserAsync(stored.UserId, ipAddress, ct);
            await unitOfWork.SaveChangesAsync(ct);
            throw new ForbiddenOperationException("El refresh token ya fue utilizado. Por seguridad se cerraron todas las sesiones activas.");
        }

        if (stored.ExpiresAtUtc <= now)
        {
            throw new ForbiddenOperationException("El refresh token ha expirado.");
        }

        var user = await userRepository.GetByIdAsync(stored.UserId, includeDetails: true, ct)
            ?? throw new ForbiddenOperationException("El usuario asociado al token ya no existe.");

        if (!user.IsActive)
        {
            throw new ForbiddenOperationException("La cuenta está desactivada.");
        }

        var roles = user.UserRoles.Select(ur => ur.Role).ToList();
        return await IssueTokensAsync(user, roles, ipAddress, userAgent, tokenToRotate: stored, ct);
    }

    public async Task LogoutAsync(string? refreshToken, string? ipAddress, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var hash = tokenService.HashRefreshToken(refreshToken);
        var stored = await refreshTokenRepository.GetByTokenHashAsync(hash, ct);
        if (stored is null || stored.RevokedAtUtc is not null)
        {
            return;
        }

        stored.RevokedAtUtc = dateTimeProvider.UtcNow;
        stored.RevokedByIp = ipAddress;
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<AuthenticatedUserDto> GetCurrentUserAsync(Guid userId, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(userId, includeDetails: true, ct)
            ?? throw new NotFoundException(nameof(User), userId);

        var roles = user.UserRoles.Select(ur => ur.Role).ToList();
        return ToAuthenticatedUserDto(user, roles);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, string? ipAddress, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(userId, includeDetails: false, ct)
            ?? throw new NotFoundException(nameof(User), userId);

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new BusinessRuleException("La contraseña actual no es correcta.");
        }

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.SecurityStamp = Guid.NewGuid();
        user.MustChangePassword = false;

        await refreshTokenRepository.RevokeAllActiveForUserAsync(userId, ipAddress, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<LoginResult> IssueTokensAsync(
        User user,
        IReadOnlyList<Role> roles,
        string? ipAddress,
        string? userAgent,
        RefreshToken? tokenToRotate,
        CancellationToken ct)
    {
        var now = dateTimeProvider.UtcNow;
        var roleNames = roles.Select(r => r.Name).Distinct().ToList();
        var permissions = roles.SelectMany(r => r.RolePermissions.Select(rp => rp.Permission.Code)).Distinct().ToList();
        var maxRoleLevel = roles.Count == 0 ? 0 : roles.Max(r => r.Level);

        var accessToken = tokenService.GenerateAccessToken(user, roleNames, permissions, maxRoleLevel);
        var rawRefreshToken = tokenService.GenerateRefreshToken();
        var newHash = tokenService.HashRefreshToken(rawRefreshToken);

        if (tokenToRotate is not null)
        {
            tokenToRotate.RevokedAtUtc = now;
            tokenToRotate.RevokedByIp = ipAddress;
            tokenToRotate.ReplacedByTokenHash = newHash;
        }

        refreshTokenRepository.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = newHash,
            ExpiresAtUtc = now.Add(RefreshTokenLifetime),
            CreatedAtUtc = now,
            CreatedByIp = ipAddress,
            UserAgent = userAgent,
        });

        await unitOfWork.SaveChangesAsync(ct);

        return new LoginResult(
            accessToken.Token,
            accessToken.ExpiresInSeconds,
            rawRefreshToken,
            ToAuthenticatedUserDto(user, roles, roleNames, permissions));
    }

    private static AuthenticatedUserDto ToAuthenticatedUserDto(User user, IReadOnlyList<Role> roles) =>
        ToAuthenticatedUserDto(
            user,
            roles,
            roles.Select(r => r.Name).Distinct().ToList(),
            roles.SelectMany(r => r.RolePermissions.Select(rp => rp.Permission.Code)).Distinct().ToList());

    private static AuthenticatedUserDto ToAuthenticatedUserDto(
        User user, IReadOnlyList<Role> roles, IReadOnlyList<string> roleNames, IReadOnlyList<string> permissions) =>
        new(
            user.Id,
            user.UserName,
            user.Email,
            user.Profile?.FullName is { Length: > 0 } fullName ? fullName : user.UserName,
            roleNames,
            permissions,
            user.MustChangePassword);

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
}
