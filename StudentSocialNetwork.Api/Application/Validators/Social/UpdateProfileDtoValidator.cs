using FluentValidation;
using StudentSocialNetwork.Api.Application.DTOs.Social;

namespace StudentSocialNetwork.Api.Application.Validators.Social;

public class UpdateProfileDtoValidator : AbstractValidator<UpdateProfileDto>
{
    public UpdateProfileDtoValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.FullName).MaximumLength(200);
        RuleFor(x => x.AvatarUrl).MaximumLength(1000);
        RuleFor(x => x.Bio).MaximumLength(1000);
        RuleFor(x => x.ClassName).MaximumLength(100);
        RuleFor(x => x.Major).MaximumLength(150);
        RuleFor(x => x.Interests).MaximumLength(2000);
    }
}
