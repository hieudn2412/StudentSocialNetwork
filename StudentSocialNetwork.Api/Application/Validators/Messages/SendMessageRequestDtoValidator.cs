using FluentValidation;
using StudentSocialNetwork.Api.Application.DTOs.Messages;

namespace StudentSocialNetwork.Api.Application.Validators.Messages;

public class SendMessageRequestDtoValidator : AbstractValidator<SendMessageRequestDto>
{
    public SendMessageRequestDtoValidator()
    {
        RuleFor(x => x.MessageType)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.Content)
            .NotEmpty()
            .When(x => x.Attachments.Count == 0)
            .WithMessage("Message content is required when there are no attachments.");

        RuleFor(x => x.Content)
            .MaximumLength(5000)
            .When(x => !string.IsNullOrWhiteSpace(x.Content));

        RuleForEach(x => x.Attachments).SetValidator(new MessageAttachmentInputDtoValidator());
    }
}
