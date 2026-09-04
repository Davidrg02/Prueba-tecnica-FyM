using FyM.Users.Application.Abstractions;
using FyM.Users.Application.Common;
using FyM.Users.Domain.Entities;
using FyM.Users.Domain.Exceptions;
using FyM.Users.Domain.Policies;

namespace FyM.Users.Application.Roles;

public sealed class RoleService(
    IRoleRepository roleRepository,
    IPermissionRepository permissionRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider,
    UserAccessPolicy accessPolicy) : IRoleService
{
    public async Task<IReadOnlyList<RoleListItemDto>> GetAllAsync(CancellationToken ct)
    {
        var roles = await roleRepository.GetAllWithUserCountsAsync(ct);
        return roles
            .Select(r => new RoleListItemDto(r.Role.Id, r.Role.Name, r.Role.Description, r.Role.Level, r.Role.IsSystem, r.UserCount))
            .ToList();
    }

    public async Task<RoleDetailDto> GetByIdAsync(int id, CancellationToken ct)
    {
        var role = await roleRepository.GetByIdAsync(id, includePermissions: true, ct)
            ?? throw new NotFoundException(nameof(Role), id);
        return ToDetailDto(role);
    }

    public async Task<RoleDetailDto> CreateAsync(CreateRoleRequest request, CancellationToken ct)
    {
        accessPolicy.EnsureCanSetRoleLevel(GetActorContext(), request.Level);

        var normalizedName = Normalize(request.Name);
        if (await roleRepository.ExistsByNameAsync(normalizedName, ct))
        {
            throw new ConflictException("Ya existe un rol con ese nombre.");
        }

        var role = new Role
        {
            Name = request.Name,
            NormalizedName = normalizedName,
            Description = request.Description,
            Level = request.Level,
            IsSystem = false,
            CreatedAtUtc = dateTimeProvider.UtcNow,
        };

        roleRepository.Add(role);
        await unitOfWork.SaveChangesAsync(ct);
        return ToDetailDto(role);
    }

    public async Task<RoleDetailDto> UpdateAsync(int id, UpdateRoleRequest request, CancellationToken ct)
    {
        var role = await roleRepository.GetByIdAsync(id, includePermissions: true, ct)
            ?? throw new NotFoundException(nameof(Role), id);

        accessPolicy.EnsureRoleIsMutable(role.IsSystem, "editarse");

        var normalizedName = Normalize(request.Name);
        if (!string.Equals(normalizedName, role.NormalizedName, StringComparison.Ordinal)
            && await roleRepository.ExistsByNameAsync(normalizedName, ct, excludeRoleId: role.Id))
        {
            throw new ConflictException("Ya existe un rol con ese nombre.");
        }

        role.Name = request.Name;
        role.NormalizedName = normalizedName;
        role.Description = request.Description;

        await unitOfWork.SaveChangesAsync(ct);
        return ToDetailDto(role);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var role = await roleRepository.GetByIdAsync(id, includePermissions: false, ct)
            ?? throw new NotFoundException(nameof(Role), id);

        accessPolicy.EnsureRoleIsMutable(role.IsSystem, "eliminarse");

        var userCount = await roleRepository.CountUsersInRoleAsync(id, ct);
        if (userCount > 0)
        {
            throw new ConflictException($"El rol tiene {userCount} usuario(s) asignado(s) y no puede eliminarse.");
        }

        roleRepository.Remove(role);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<RoleDetailDto> UpdatePermissionsAsync(int id, UpdateRolePermissionsRequest request, CancellationToken ct)
    {
        var role = await roleRepository.GetByIdAsync(id, includePermissions: true, ct)
            ?? throw new NotFoundException(nameof(Role), id);

        var distinctIds = request.PermissionIds.Distinct().ToList();
        var permissions = await permissionRepository.GetByIdsAsync(distinctIds, ct);
        if (permissions.Count != distinctIds.Count)
        {
            throw new BusinessRuleException("Uno o más permisos especificados no existen.");
        }

        role.RolePermissions.Clear();
        foreach (var permission in permissions)
        {
            role.RolePermissions.Add(new RolePermission { Role = role, Permission = permission });
        }

        await unitOfWork.SaveChangesAsync(ct);
        return ToDetailDto(role);
    }

    private ActorContext GetActorContext() => new(currentUser.UserId, currentUser.MaxRoleLevel, currentUser.IsSuperAdmin);

    private static RoleDetailDto ToDetailDto(Role role) => new(
        role.Id,
        role.Name,
        role.Description,
        role.Level,
        role.IsSystem,
        role.RolePermissions
            .Select(rp => new PermissionDto(rp.Permission.Id, rp.Permission.Code, rp.Permission.Module, rp.Permission.Description))
            .ToList());

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
}
