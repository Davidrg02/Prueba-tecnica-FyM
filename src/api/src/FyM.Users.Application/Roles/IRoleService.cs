namespace FyM.Users.Application.Roles;

public interface IRoleService
{
    Task<IReadOnlyList<RoleListItemDto>> GetAllAsync(CancellationToken ct);
    Task<RoleDetailDto> GetByIdAsync(int id, CancellationToken ct);
    Task<RoleDetailDto> CreateAsync(CreateRoleRequest request, CancellationToken ct);
    Task<RoleDetailDto> UpdateAsync(int id, UpdateRoleRequest request, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
    Task<RoleDetailDto> UpdatePermissionsAsync(int id, UpdateRolePermissionsRequest request, CancellationToken ct);
}
