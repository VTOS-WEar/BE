using FluentValidation;
using VTOS.Application.Features.Account.Commands;

namespace VTOS.Application.Features.Account.Validators;

public class UpdateAccountEmailCommandValidator : AbstractValidator<UpdateAccountEmailCommand>
{
    public UpdateAccountEmailCommandValidator()
    {
        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters.")
            .EmailAddress().WithMessage("Invalid email format.");
    }
}
