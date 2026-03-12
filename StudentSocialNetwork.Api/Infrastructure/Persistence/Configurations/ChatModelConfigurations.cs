using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentSocialNetwork.Api.Domain.Entities;

namespace StudentSocialNetwork.Api.Infrastructure.Persistence.Configurations;

public static class ChatModelConfigurations
{
    public static void ApplyChatModelConfigurations(this ModelBuilder modelBuilder)
    {
        ConfigureUsers(modelBuilder.Entity<User>());
        ConfigureExternalAccounts(modelBuilder.Entity<ExternalAccount>());
        ConfigureConversations(modelBuilder.Entity<Conversation>());
        ConfigureConversationMembers(modelBuilder.Entity<ConversationMember>());
        ConfigureMessages(modelBuilder.Entity<Message>());
        ConfigureMessageAttachments(modelBuilder.Entity<MessageAttachment>());
        ConfigureMessageReactions(modelBuilder.Entity<MessageReaction>());
        ConfigureMessageReads(modelBuilder.Entity<MessageRead>());
        ConfigurePinnedMessages(modelBuilder.Entity<PinnedMessage>());
        ConfigureConversationSettings(modelBuilder.Entity<ConversationSetting>());
        ConfigureRefreshTokens(modelBuilder.Entity<RefreshToken>());
    }

    private static void ConfigureUsers(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Username).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(255).IsRequired();
        builder.Property(x => x.AvatarUrl).HasMaxLength(500);
        builder.Property(x => x.Bio).HasMaxLength(500);
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime");
        builder.Property(x => x.LastActiveAt).HasColumnType("datetime");

        builder.HasIndex(x => x.Email).IsUnique();
    }

    private static void ConfigureExternalAccounts(EntityTypeBuilder<ExternalAccount> builder)
    {
        builder.ToTable("ExternalAccounts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Provider).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ProviderUserId).HasMaxLength(255).IsRequired();
        builder.Property(x => x.AccessToken).HasMaxLength(1000);
        builder.Property(x => x.CreatedAt).HasColumnType("datetime");

        builder.HasIndex(x => new { x.Provider, x.ProviderUserId }).IsUnique();

        builder.HasOne(x => x.User)
            .WithMany(x => x.ExternalAccounts)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureConversations(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(255);
        builder.Property(x => x.AvatarUrl).HasMaxLength(500);
        builder.Property(x => x.CreatedAt).HasColumnType("datetime");
        builder.Property(x => x.LastMessageAt).HasColumnType("datetime");

        builder.HasOne(x => x.Creator)
            .WithMany(x => x.CreatedConversations)
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.LastMessage)
            .WithMany()
            .HasForeignKey(x => x.LastMessageId)
            .OnDelete(DeleteBehavior.NoAction);
    }

    private static void ConfigureConversationMembers(EntityTypeBuilder<ConversationMember> builder)
    {
        builder.ToTable("ConversationMembers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Role).HasMaxLength(20).IsRequired();
        builder.Property(x => x.JoinedAt).HasColumnType("datetime");
        builder.Property(x => x.LeftAt).HasColumnType("datetime");
        builder.Property(x => x.MutedUntil).HasColumnType("datetime");

        builder.HasIndex(x => new { x.ConversationId, x.UserId }).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.ConversationId });

        builder.HasOne(x => x.Conversation)
            .WithMany(x => x.Members)
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany(x => x.ConversationMembers)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.LastReadMessage)
            .WithMany(x => x.LastReadByMembers)
            .HasForeignKey(x => x.LastReadMessageId)
            .OnDelete(DeleteBehavior.NoAction);
    }

    private static void ConfigureMessages(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.MessageType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime");
        builder.Property(x => x.EditedAt).HasColumnType("datetime");
        builder.Property(x => x.DeletedAt).HasColumnType("datetime");

        builder.HasIndex(x => new { x.ConversationId, x.CreatedAt }).IsDescending(false, true);

        builder.HasOne(x => x.Conversation)
            .WithMany(x => x.Messages)
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Sender)
            .WithMany(x => x.SentMessages)
            .HasForeignKey(x => x.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReplyToMessage)
            .WithMany(x => x.Replies)
            .HasForeignKey(x => x.ReplyToMessageId)
            .OnDelete(DeleteBehavior.NoAction);
    }

    private static void ConfigureMessageAttachments(EntityTypeBuilder<MessageAttachment> builder)
    {
        builder.ToTable("MessageAttachments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileUrl).HasMaxLength(500).IsRequired();
        builder.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.FileType).HasMaxLength(50).IsRequired();

        builder.HasOne(x => x.Message)
            .WithMany(x => x.Attachments)
            .HasForeignKey(x => x.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureMessageReactions(EntityTypeBuilder<MessageReaction> builder)
    {
        builder.ToTable("MessageReactions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReactionType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime");

        builder.HasIndex(x => new { x.MessageId, x.UserId, x.ReactionType }).IsUnique();
        builder.HasIndex(x => x.MessageId);

        builder.HasOne(x => x.Message)
            .WithMany(x => x.Reactions)
            .HasForeignKey(x => x.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany(x => x.MessageReactions)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureMessageReads(EntityTypeBuilder<MessageRead> builder)
    {
        builder.ToTable("MessageReads");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SeenAt).HasColumnType("datetime");

        builder.HasIndex(x => new { x.MessageId, x.UserId }).IsUnique();

        builder.HasOne(x => x.Message)
            .WithMany(x => x.Reads)
            .HasForeignKey(x => x.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany(x => x.MessageReads)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigurePinnedMessages(EntityTypeBuilder<PinnedMessage> builder)
    {
        builder.ToTable("PinnedMessages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PinnedAt).HasColumnType("datetime");

        builder.HasIndex(x => new { x.ConversationId, x.MessageId }).IsUnique();

        builder.HasOne(x => x.Conversation)
            .WithMany(x => x.PinnedMessages)
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Message)
            .WithMany(x => x.PinnedByConversations)
            .HasForeignKey(x => x.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.PinnedByUser)
            .WithMany(x => x.PinnedMessages)
            .HasForeignKey(x => x.PinnedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureConversationSettings(EntityTypeBuilder<ConversationSetting> builder)
    {
        builder.ToTable("ConversationSettings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Theme).HasMaxLength(50);
        builder.Property(x => x.NotificationLevel).HasMaxLength(50);

        builder.HasIndex(x => new { x.ConversationId, x.UserId }).IsUnique();

        builder.HasOne(x => x.Conversation)
            .WithMany(x => x.Settings)
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany(x => x.ConversationSettings)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureRefreshTokens(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ReplacedByTokenHash).HasMaxLength(128);
        builder.Property(x => x.CreatedByIp).HasMaxLength(64);
        builder.Property(x => x.ExpiresAt).HasColumnType("datetime");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime");
        builder.Property(x => x.RevokedAt).HasColumnType("datetime");

        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.ExpiresAt }).IsDescending(false, true);

        builder.HasOne(x => x.User)
            .WithMany(x => x.RefreshTokens)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
