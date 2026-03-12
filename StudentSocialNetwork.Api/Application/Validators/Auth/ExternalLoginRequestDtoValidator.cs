using FluentValidation;
using StudentSocialNetwork.Api.Application.DTOs.Auth;

namespace StudentSocialNetwork.Api.Application.Validators.Auth;

public class ExternalLoginRequestDtoValidator : AbstractValidator<ExternalLoginRequestDto>
{
    public ExternalLoginRequestDtoValidator()
    {
        RuleFor(x => x.Provider).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ProviderUserId).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Username).MaximumLength(100);
        RuleFor(x => x.AvatarUrl)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.AvatarUrl));
        RuleFor(x => x.AccessToken)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrWhiteSpace(x.AccessToken));
    }
}
