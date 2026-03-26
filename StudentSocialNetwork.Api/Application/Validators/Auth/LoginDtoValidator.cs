using FluentValidation;
using StudentSocialNetwork.Api.Application.DTOs.Auth;

namespace StudentSocialNetwork.Api.Application.Validators.Auth;

public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(200);
    }
}
