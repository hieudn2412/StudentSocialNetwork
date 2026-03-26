using FluentValidation;
using StudentSocialNetwork.Api.Application.DTOs.Auth;

namespace StudentSocialNetwork.Api.Application.Validators.Auth;

public class ChangePasswordDtoValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordDtoValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().MinimumLength(6).MaximumLength(200);
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6).MaximumLength(200);
    }
}
