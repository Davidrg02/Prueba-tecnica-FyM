using FyM.Users.Application.Common;
using Microsoft.AspNetCore.Authorization;

namespace FyM.Users.Api.Authorization;

public sealed class PermissionRequirement(string permissionCode) : IAuthorizationRequirement
{
    public string PermissionCode { get; } = permissionCode;
}

/// <summary>Aprueba una <see cref="PermissionRequirement"/> comparando el
/// código de permiso contra los claims <c>permission</c> del JWT ya
/// validado — sin volver a consultar la base de datos.</summary>
public sealed class PermissionAuthorizationHandler(ICurrentUser currentUser) : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (currentUser.HasPermission(requirement.PermissionCode))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
