using FluentValidation;
using StudentSocialNetwork.Api.Application.DTOs.Conversations;

namespace StudentSocialNetwork.Api.Application.Validators.Conversations;

public class CreateGroupConversationRequestDtoValidator : AbstractValidator<CreateGroupConversationRequestDto>
{
    public CreateGroupConversationRequestDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);

        RuleFor(x => x.MemberIds)
            .NotEmpty()
            .WithMessage("At least one member is required.");

        RuleForEach(x => x.MemberIds)
            .GreaterThan(0);

        RuleFor(x => x.AvatarUrl)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.AvatarUrl));
    }
}
