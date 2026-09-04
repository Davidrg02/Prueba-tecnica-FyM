namespace FyM.Users.Application.Users;

public sealed record RoleSummaryDto(int Id, string Name, int Level);

public sealed record UserListItemDto(
    Guid Id,
    string UserName,
    string Email,
    string FullName,
    bool IsActive,
    bool IsSystem,
    IReadOnlyList<RoleSummaryDto> Roles,
    DateTime? LastLoginUtc,
    DateTime CreatedAtUtc);

public sealed record UserDetailDto(
    Guid Id,
    string UserName,
    string Email,
    string FirstName,
    string? MiddleName,
    string LastName,
    string? SecondLastName,
    int? DocumentTypeId,
    string? DocumentNumber,
    string? PhoneNumber,
    bool IsActive,
    bool IsSystem,
    bool MustChangePassword,
    IReadOnlyList<RoleSummaryDto> Roles,
    DateTime? LastLoginUtc,
    DateTime CreatedAtUtc);

public sealed record CreateUserRequest(
    string UserName,
    string Email,
    string Password,
    string FirstName,
    string? MiddleName,
    string LastName,
    string? SecondLastName,
    int? DocumentTypeId,
    string? DocumentNumber,
    string? PhoneNumber,
    IReadOnlyList<int> RoleIds);

public sealed record UpdateUserRequest(
    string Email,
    string FirstName,
    string? MiddleName,
    string LastName,
    string? SecondLastName,
    int? DocumentTypeId,
    string? DocumentNumber,
    string? PhoneNumber);

public sealed record UpdateUserStatusRequest(bool IsActive);

public sealed record AssignRolesRequest(IReadOnlyList<int> RoleIds);

public sealed record ResetPasswordRequest(string NewPassword);

public sealed record UpdateOwnProfileRequest(
    string FirstName,
    string? MiddleName,
    string LastName,
    string? SecondLastName,
    string? PhoneNumber);
