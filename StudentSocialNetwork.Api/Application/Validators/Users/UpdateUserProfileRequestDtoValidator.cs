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

        RuleFor(x => x.FullName)
            .MaximumLength(200);

        RuleFor(x => x.Bio)
            .MaximumLength(1000);

        RuleFor(x => x.AvatarUrl)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrWhiteSpace(x.AvatarUrl));

        RuleFor(x => x.ClassName)
            .MaximumLength(100);

        RuleFor(x => x.Major)
            .MaximumLength(150);

        RuleFor(x => x.Interests)
            .MaximumLength(2000);
    }
}
