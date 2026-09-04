namespace FyM.Users.Application.Common;

/// <summary>Abstrae la hora actual para que los servicios sean testeables sin depender del reloj real.</summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
