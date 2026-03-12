using FluentValidation;
using StudentSocialNetwork.Api.Application.DTOs.Messages;

namespace StudentSocialNetwork.Api.Application.Validators.Messages;

public class AddMessageReactionRequestDtoValidator : AbstractValidator<AddMessageReactionRequestDto>
{
    public AddMessageReactionRequestDtoValidator()
    {
        RuleFor(x => x.ReactionType)
            .NotEmpty()
            .MaximumLength(20);
    }
}
