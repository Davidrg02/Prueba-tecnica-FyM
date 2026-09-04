using FyM.Users.Application.Auth;
using FyM.Users.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FyM.Users.Api.Controllers;

/// <summary>Respuesta de autenticación devuelta al cliente. El refresh
/// token nunca viaja aquí: se entrega únicamente en una cookie HttpOnly.</summary>
public sealed record AuthResponse(string AccessToken, int ExpiresInSeconds, AuthenticatedUserDto User);

/// <summary>Registro, autenticación y gestión de sesión.</summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class AuthController(IAuthService authService, ICurrentUser currentUser, IConfiguration configuration) : ControllerBase
{
    private const string RefreshTokenCookieName = "refresh_token";
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    /// <summary>Registra un nuevo usuario con el rol "User" (si el auto-registro está habilitado).</summary>
    /// <remarks>La respuesta incluye el access token y establece la cookie HttpOnly <c>refresh_token</c>. El endpoint está limitado por la política de rate limiting de autenticación.</remarks>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await authService.RegisterAsync(request, GetIpAddress(), GetUserAgent(), ct);
        return Ok(BuildResponse(result));
    }

    /// <summary>Autentica un usuario y devuelve un access token; el refresh token viaja en una cookie HttpOnly.</summary>
    /// <remarks>Use el correo y la contraseña registrados. Una cuenta desactivada o temporalmente bloqueada no puede autenticarse.</remarks>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, GetIpAddress(), GetUserAgent(), ct);
        return Ok(BuildResponse(result));
    }

    /// <summary>Rota el refresh token de la cookie y emite un nuevo access token (silent refresh).</summary>
    /// <remarks>La cookie debe pertenecer a una sesión válida. La rotación invalida el refresh token anterior y establece uno nuevo.</remarks>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken ct)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Unauthorized();
        }

        var result = await authService.RefreshAsync(refreshToken, GetIpAddress(), GetUserAgent(), ct);
        return Ok(BuildResponse(result));
    }

    /// <summary>Revoca el refresh token actual y elimina la cookie de sesión.</summary>
    /// <remarks>Requiere un access token válido. Si no existe un refresh token, la sesión se considera cerrada igualmente.</remarks>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];
        await authService.LogoutAsync(refreshToken, GetIpAddress(), ct);
        Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions { Path = "/api/v1/auth" });
        return NoContent();
    }

    /// <summary>Datos del usuario autenticado actual, con sus roles y permisos.</summary>
    /// <remarks>El usuario se identifica a partir del subject (<c>sub</c>) del JWT; no es necesario enviar un identificador en la URL.</remarks>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(AuthenticatedUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthenticatedUserDto>> Me(CancellationToken ct)
    {
        var dto = await authService.GetCurrentUserAsync(currentUser.UserId, ct);
        return Ok(dto);
    }

    /// <summary>Cambia la contraseña del usuario autenticado y revoca todas sus sesiones activas.</summary>
    /// <remarks>La nueva contraseña se valida según las reglas de seguridad configuradas. Tras completarse, debe iniciar sesión de nuevo porque se invalidan las cookies de refresh existentes.</remarks>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        await authService.ChangePasswordAsync(currentUser.UserId, request, GetIpAddress(), ct);
        Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions { Path = "/api/v1/auth" });
        return NoContent();
    }

    private AuthResponse BuildResponse(LoginResult result)
    {
        SetRefreshTokenCookie(result.RefreshToken);
        return new AuthResponse(result.AccessToken, result.ExpiresInSeconds, result.User);
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        Response.Cookies.Append(RefreshTokenCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = configuration.GetValue("Cookies:Secure", true),
            SameSite = SameSiteMode.Lax,
            Path = "/api/v1/auth",
            Expires = DateTimeOffset.UtcNow.Add(RefreshTokenLifetime),
        });
    }

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? GetUserAgent() => Request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua : null;
}
