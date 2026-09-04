using System.Text.RegularExpressions;

namespace FyM.Users.Application.Common;

/// <summary>
/// Política de contraseñas compartida por los validadores de registro,
/// creación y restablecimiento. Se replica en el frontend (mismo patrón)
/// para dar feedback inmediato, pero la única fuente de verdad es esta.
/// </summary>
public static partial class PasswordPolicy
{
    public const int MinLength = 8;
    public const string Description = "Mínimo 8 caracteres, con mayúscula, minúscula, dígito y símbolo.";

    public static bool IsValid(string password) =>
        password.Length >= MinLength && StrengthRegex().IsMatch(password);

    [GeneratedRegex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\w\s]).+$")]
    private static partial Regex StrengthRegex();
}
