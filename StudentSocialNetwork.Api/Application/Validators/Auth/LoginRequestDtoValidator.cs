using FluentValidation;
using StudentSocialNetwork.Api.Application.DTOs.Auth;

namespace StudentSocialNetwork.Api.Application.Validators.Auth;

public class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Username)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Username));
    }
}
