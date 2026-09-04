using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FyM.Users.Api.Filters;

/// <summary>
/// Valida automáticamente cualquier argumento de acción para el que exista
/// un <see cref="IValidator{T}"/> registrado en el contenedor de DI. Desde
/// FluentValidation 11 ya no existe integración automática con MVC, así que
/// este filtro global reemplaza a la extensión <c>AddFluentValidationAutoValidation</c>
/// sin añadir una dependencia de terceros. Los errores se lanzan como
/// <see cref="ValidationException"/>, que <c>GlobalExceptionHandler</c> ya
/// traduce a 400 con el detalle por campo.
/// </summary>
public sealed class ValidationActionFilter(IServiceProvider serviceProvider) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (serviceProvider.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);
            if (!result.IsValid)
            {
                throw new ValidationException(result.Errors);
            }
        }

        await next();
    }
}
