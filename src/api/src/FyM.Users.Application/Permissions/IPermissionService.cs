using FyM.Users.Application.Roles;

namespace FyM.Users.Application.Permissions;

public sealed record PermissionGroupDto(string Module, IReadOnlyList<PermissionDto> Permissions);

public interface IPermissionService
{
    Task<IReadOnlyList<PermissionGroupDto>> GetGroupedAsync(CancellationToken ct);
}
