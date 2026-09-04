namespace FyM.Users.Domain.Exceptions;

/// <summary>
/// Base de todas las excepciones que representan una violación de una
/// regla del dominio. El middleware global las traduce a <c>ProblemDetails</c>
/// sin exponer detalles internos.
/// </summary>
public abstract class DomainException(string message) : Exception(message);

/// <summary>La entidad solicitada no existe o fue eliminada.</summary>
public sealed class NotFoundException(string entityName, object key)
    : DomainException($"{entityName} con identificador '{key}' no fue encontrado.");

/// <summary>La operación entra en conflicto con el estado actual de los datos
/// (duplicados, concurrencia optimista, dependencias existentes).</summary>
public sealed class ConflictException(string message) : DomainException(message);

/// <summary>Los datos de entrada violan una regla de negocio del dominio
/// que no es un simple error de validación de formato.</summary>
public sealed class BusinessRuleException(string message) : DomainException(message);

/// <summary>El actor autenticado no tiene permitido realizar la operación
/// sobre el recurso objetivo, según <see cref="Policies.UserAccessPolicy"/>.</summary>
public sealed class ForbiddenOperationException(string message) : DomainException(message);
