using FluentValidation;
using FyM.Users.Application.Common;

namespace FyM.Users.Application.Auth;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty()
            .Must(PasswordPolicy.IsValid).WithMessage(PasswordPolicy.Description);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DocumentNumber).MaximumLength(30);
        RuleFor(x => x.PhoneNumber).MaximumLength(30);
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty()
            .Must(PasswordPolicy.IsValid).WithMessage(PasswordPolicy.Description)
            .NotEqual(x => x.CurrentPassword).WithMessage("La nueva contraseña debe ser diferente de la actual.");
    }
}
