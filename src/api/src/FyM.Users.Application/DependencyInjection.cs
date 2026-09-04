using FluentValidation;
using FyM.Users.Application.Auth;
using FyM.Users.Application.Catalogs;
using FyM.Users.Application.Permissions;
using FyM.Users.Application.Roles;
using FyM.Users.Application.Users;
using FyM.Users.Domain.Policies;
using Microsoft.Extensions.DependencyInjection;

namespace FyM.Users.Application;

/// <summary>Registro de todos los servicios de la capa Application. Se
/// invoca desde <c>Program.cs</c> junto con <c>AddInfrastructure</c>.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

        services.AddSingleton<UserAccessPolicy>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<ICatalogService, CatalogService>();

        return services;
    }
}
