using FyM.Users.Application.Abstractions;
using FyM.Users.Application.Roles;

namespace FyM.Users.Application.Permissions;

public sealed class PermissionService(IPermissionRepository permissionRepository) : IPermissionService
{
    public async Task<IReadOnlyList<PermissionGroupDto>> GetGroupedAsync(CancellationToken ct)
    {
        var permissions = await permissionRepository.GetAllAsync(ct);

        return permissions
            .GroupBy(p => p.Module)
            .OrderBy(g => g.Key)
            .Select(g => new PermissionGroupDto(
                g.Key,
                g.Select(p => new PermissionDto(p.Id, p.Code, p.Module, p.Description)).ToList()))
            .ToList();
    }
}
