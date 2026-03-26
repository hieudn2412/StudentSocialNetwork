using FluentValidation;
using StudentSocialNetwork.Api.Application.DTOs.Social;

namespace StudentSocialNetwork.Api.Application.Validators.Social;

public class CreateCommentDtoValidator : AbstractValidator<CreateCommentDto>
{
    public CreateCommentDtoValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(2000);
    }
}
