using FluentValidation;
using StudentSocialNetwork.Api.Application.DTOs.Users;

namespace StudentSocialNetwork.Api.Application.Validators.Users;

public class UpdateUserProfileRequestDtoValidator : AbstractValidator<UpdateUserProfileRequestDto>
{
    public UpdateUserProfileRequestDtoValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Bio)
            .MaximumLength(500);

        RuleFor(x => x.AvatarUrl)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.AvatarUrl));
    }
}
