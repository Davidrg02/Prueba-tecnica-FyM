namespace FyM.Users.Application.Roles;

public sealed record PermissionDto(int Id, string Code, string Module, string? Description);

public sealed record RoleListItemDto(int Id, string Name, string? Description, int Level, bool IsSystem, int UserCount);

public sealed record RoleDetailDto(
    int Id,
    string Name,
    string? Description,
    int Level,
    bool IsSystem,
    IReadOnlyList<PermissionDto> Permissions);

public sealed record CreateRoleRequest(string Name, string? Description, int Level);

public sealed record UpdateRoleRequest(string Name, string? Description);

public sealed record UpdateRolePermissionsRequest(IReadOnlyList<int> PermissionIds);
