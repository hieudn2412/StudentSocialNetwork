using FluentValidation;
using StudentSocialNetwork.Api.Application.DTOs.Social;

namespace StudentSocialNetwork.Api.Application.Validators.Social;

public class CreatePostDtoValidator : AbstractValidator<CreatePostDto>
{
    public CreatePostDtoValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(4000);

        RuleFor(x => x.ImageUrl)
            .MaximumLength(1000);
    }
}
