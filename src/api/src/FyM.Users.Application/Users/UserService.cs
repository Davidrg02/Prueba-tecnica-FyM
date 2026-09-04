using FyM.Users.Application.Abstractions;
using FyM.Users.Application.Common;
using FyM.Users.Domain.Entities;
using FyM.Users.Domain.Enums;
using FyM.Users.Domain.Exceptions;
using FyM.Users.Domain.Policies;

namespace FyM.Users.Application.Users;

/// <summary>
/// CRUD de usuarios. Toda operación de escritura pasa primero por
/// <see cref="UserAccessPolicy"/> para aplicar las reglas de jerarquía de
/// roles antes de tocar la base de datos.
/// </summary>
public sealed class UserService(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider,
    UserAccessPolicy accessPolicy) : IUserService
{
    public async Task<PagedResult<UserListItemDto>> GetPagedAsync(UserQueryParameters parameters, CancellationToken ct)
    {
        var (items, total) = await userRepository.GetPagedAsync(parameters, ct);
        var dtos = items.Select(ToListItemDto).ToList();
        return PagedResult<UserListItemDto>.Create(dtos, parameters.Page, parameters.PageSize, total);
    }

    public async Task<UserDetailDto> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(id, includeDetails: true, ct)
            ?? throw new NotFoundException(nameof(User), id);
        return ToDetailDto(user);
    }

    public async Task<UserDetailDto> CreateAsync(CreateUserRequest request, CancellationToken ct)
    {
        var normalizedEmail = Normalize(request.Email);
        if (await userRepository.ExistsByEmailAsync(normalizedEmail, ct))
        {
            throw new ConflictException("Ya existe un usuario registrado con ese correo electrónico.");
        }

        if (await userRepository.ExistsByUserNameAsync(request.UserName, ct))
        {
            throw new ConflictException("Ya existe un usuario con ese nombre de usuario.");
        }

        var roles = await ResolveRolesAsync(request.RoleIds, ct);
        var maxRequestedLevel = roles.Count == 0 ? 0 : roles.Max(r => r.Level);
        accessPolicy.EnsureCanGrantRoleLevel(GetActorContext(), maxRequestedLevel);

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
                MiddleName = request.MiddleName,
                LastName = request.LastName,
                SecondLastName = request.SecondLastName,
                DocumentTypeId = request.DocumentTypeId,
                DocumentNumber = request.DocumentNumber,
                PhoneNumber = request.PhoneNumber,
            },
        };

        foreach (var role in roles)
        {
            user.UserRoles.Add(new UserRole { Role = role, AssignedAtUtc = now, AssignedByUserId = currentUser.UserId });
        }

        userRepository.Add(user);
        await unitOfWork.SaveChangesAsync(ct);

        return ToDetailDto(user);
    }

    public async Task<UserDetailDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(id, includeDetails: true, ct)
            ?? throw new NotFoundException(nameof(User), id);

        accessPolicy.EnsureCanModify(GetActorContext(), GetTargetContext(user));

        var normalizedEmail = Normalize(request.Email);
        if (!string.Equals(normalizedEmail, user.NormalizedEmail, StringComparison.Ordinal)
            && await userRepository.ExistsByEmailAsync(normalizedEmail, ct, excludeUserId: user.Id))
        {
            throw new ConflictException("Ya existe un usuario registrado con ese correo electrónico.");
        }

        user.Email = request.Email;
        user.NormalizedEmail = normalizedEmail;

        user.Profile ??= new UserProfile { UserId = user.Id };
        user.Profile.FirstName = request.FirstName;
        user.Profile.MiddleName = request.MiddleName;
        user.Profile.LastName = request.LastName;
        user.Profile.SecondLastName = request.SecondLastName;
        user.Profile.DocumentTypeId = request.DocumentTypeId;
        user.Profile.DocumentNumber = request.DocumentNumber;
        user.Profile.PhoneNumber = request.PhoneNumber;

        await unitOfWork.SaveChangesAsync(ct);
        return ToDetailDto(user);
    }

    public async Task SetStatusAsync(Guid id, UpdateUserStatusRequest request, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(id, includeDetails: true, ct)
            ?? throw new NotFoundException(nameof(User), id);

        if (request.IsActive)
        {
            accessPolicy.EnsureCanModify(GetActorContext(), GetTargetContext(user));
        }
        else
        {
            var isLastSuperAdmin = await IsLastActiveSuperAdminAsync(user, ct);
            accessPolicy.EnsureCanDeactivateOrDelete(GetActorContext(), GetTargetContext(user), isLastSuperAdmin);
        }

        user.IsActive = request.IsActive;

        if (!request.IsActive)
        {
            await refreshTokenRepository.RevokeAllActiveForUserAsync(user.Id, revokedByIp: null, ct);
        }

        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(id, includeDetails: true, ct)
            ?? throw new NotFoundException(nameof(User), id);

        var isLastSuperAdmin = await IsLastActiveSuperAdminAsync(user, ct);
        accessPolicy.EnsureCanDeactivateOrDelete(GetActorContext(), GetTargetContext(user), isLastSuperAdmin);

        var now = dateTimeProvider.UtcNow;
        user.IsDeleted = true;
        user.DeletedAtUtc = now;
        user.IsActive = false;

        await refreshTokenRepository.RevokeAllActiveForUserAsync(user.Id, revokedByIp: null, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<UserDetailDto> AssignRolesAsync(Guid id, AssignRolesRequest request, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(id, includeDetails: true, ct)
            ?? throw new NotFoundException(nameof(User), id);

        var roles = await ResolveRolesAsync(request.RoleIds, ct);
        var maxRequestedLevel = roles.Count == 0 ? 0 : roles.Max(r => r.Level);

        accessPolicy.EnsureCanAssignRoles(GetActorContext(), GetTargetContext(user), maxRequestedLevel);

        var currentlyHasSuperAdmin = user.UserRoles.Any(ur => ur.Role.Name == SystemRole.SuperAdmin);
        var willHaveSuperAdmin = roles.Any(r => r.Name == SystemRole.SuperAdmin);
        if (currentlyHasSuperAdmin && !willHaveSuperAdmin && await IsLastActiveSuperAdminAsync(user, ct))
        {
            throw new ForbiddenOperationException("No puede retirar el rol de super administrador al único super administrador activo del sistema.");
        }

        var now = dateTimeProvider.UtcNow;
        user.UserRoles.Clear();
        foreach (var role in roles)
        {
            user.UserRoles.Add(new UserRole { Role = role, AssignedAtUtc = now, AssignedByUserId = currentUser.UserId });
        }

        // Los permisos del usuario cambiaron: se invalidan los access tokens ya emitidos.
        user.SecurityStamp = Guid.NewGuid();

        await unitOfWork.SaveChangesAsync(ct);
        return ToDetailDto(user);
    }

    public async Task ResetPasswordAsync(Guid id, ResetPasswordRequest request, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(id, includeDetails: true, ct)
            ?? throw new NotFoundException(nameof(User), id);

        accessPolicy.EnsureCanResetPassword(GetActorContext(), GetTargetContext(user));

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.SecurityStamp = Guid.NewGuid();
        user.MustChangePassword = true;

        await refreshTokenRepository.RevokeAllActiveForUserAsync(user.Id, revokedByIp: null, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<UserDetailDto> GetOwnProfileAsync(CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId, includeDetails: true, ct)
            ?? throw new NotFoundException(nameof(User), currentUser.UserId);
        return ToDetailDto(user);
    }

    public async Task<UserDetailDto> UpdateOwnProfileAsync(UpdateOwnProfileRequest request, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId, includeDetails: true, ct)
            ?? throw new NotFoundException(nameof(User), currentUser.UserId);

        user.Profile ??= new UserProfile { UserId = user.Id };
        user.Profile.FirstName = request.FirstName;
        user.Profile.MiddleName = request.MiddleName;
        user.Profile.LastName = request.LastName;
        user.Profile.SecondLastName = request.SecondLastName;
        user.Profile.PhoneNumber = request.PhoneNumber;

        await unitOfWork.SaveChangesAsync(ct);
        return ToDetailDto(user);
    }

    private async Task<IReadOnlyList<Role>> ResolveRolesAsync(IReadOnlyList<int> roleIds, CancellationToken ct)
    {
        var distinctIds = roleIds.Distinct().ToList();
        var roles = await roleRepository.GetByIdsAsync(distinctIds, ct);
        if (roles.Count != distinctIds.Count)
        {
            throw new BusinessRuleException("Uno o más roles especificados no existen.");
        }

        return roles;
    }

    private async Task<bool> IsLastActiveSuperAdminAsync(User user, CancellationToken ct)
    {
        var isSuperAdmin = user.UserRoles.Any(ur => ur.Role.Name == SystemRole.SuperAdmin);
        if (!isSuperAdmin || !user.IsActive)
        {
            return false;
        }

        var otherActiveSuperAdmins = await userRepository.CountActiveSuperAdminsAsync(ct, excludeUserId: user.Id);
        return otherActiveSuperAdmins == 0;
    }

    private ActorContext GetActorContext() => new(currentUser.UserId, currentUser.MaxRoleLevel, currentUser.IsSuperAdmin);

    private static TargetUserContext GetTargetContext(User user) =>
        new(user.Id, user.UserRoles.Count == 0 ? 0 : user.UserRoles.Max(ur => ur.Role.Level), user.IsSystem);

    private static UserListItemDto ToListItemDto(User user) => new(
        user.Id,
        user.UserName,
        user.Email,
        user.Profile?.FullName is { Length: > 0 } fullName ? fullName : user.UserName,
        user.IsActive,
        user.IsSystem,
        user.UserRoles.Select(ur => new RoleSummaryDto(ur.Role.Id, ur.Role.Name, ur.Role.Level)).ToList(),
        user.LastLoginUtc,
        user.CreatedAtUtc);

    private static UserDetailDto ToDetailDto(User user) => new(
        user.Id,
        user.UserName,
        user.Email,
        user.Profile?.FirstName ?? string.Empty,
        user.Profile?.MiddleName,
        user.Profile?.LastName ?? string.Empty,
        user.Profile?.SecondLastName,
        user.Profile?.DocumentTypeId,
        user.Profile?.DocumentNumber,
        user.Profile?.PhoneNumber,
        user.IsActive,
        user.IsSystem,
        user.MustChangePassword,
        user.UserRoles.Select(ur => new RoleSummaryDto(ur.Role.Id, ur.Role.Name, ur.Role.Level)).ToList(),
        user.LastLoginUtc,
        user.CreatedAtUtc);

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
}
