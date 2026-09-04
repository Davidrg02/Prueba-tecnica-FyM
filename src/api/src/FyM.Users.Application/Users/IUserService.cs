using FyM.Users.Application.Common;

namespace FyM.Users.Application.Users;

public interface IUserService
{
    Task<PagedResult<UserListItemDto>> GetPagedAsync(UserQueryParameters parameters, CancellationToken ct);
    Task<UserDetailDto> GetByIdAsync(Guid id, CancellationToken ct);
    Task<UserDetailDto> CreateAsync(CreateUserRequest request, CancellationToken ct);
    Task<UserDetailDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct);
    Task SetStatusAsync(Guid id, UpdateUserStatusRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<UserDetailDto> AssignRolesAsync(Guid id, AssignRolesRequest request, CancellationToken ct);
    Task ResetPasswordAsync(Guid id, ResetPasswordRequest request, CancellationToken ct);
    Task<UserDetailDto> GetOwnProfileAsync(CancellationToken ct);
    Task<UserDetailDto> UpdateOwnProfileAsync(UpdateOwnProfileRequest request, CancellationToken ct);
}
