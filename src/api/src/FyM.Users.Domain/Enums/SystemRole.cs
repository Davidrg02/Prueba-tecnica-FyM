namespace FyM.Users.Domain.Enums;

/// <summary>
/// Roles precreados por el <c>DatabaseSeeder</c> y marcados como
/// <c>IsSystem = true</c> (no se pueden eliminar). El nivel jerárquico
/// (<see cref="Level"/>) es lo que permite expresar la regla "un actor no
/// puede operar sobre un usuario de nivel igual o superior" de forma
/// genérica, sin comparar nombres de rol en el código.
/// </summary>
public static class SystemRole
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string User = "User";

    public static class Level
    {
        public const int SuperAdmin = 100;
        public const int Admin = 50;
        public const int User = 10;
    }
}
