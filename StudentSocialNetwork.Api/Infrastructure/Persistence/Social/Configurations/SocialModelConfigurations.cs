using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentSocialNetwork.Api.Domain.Entities.Social;

namespace StudentSocialNetwork.Api.Infrastructure.Persistence.Social.Configurations;

public static class SocialModelConfigurations
{
    public static void ApplySocialModelConfigurations(this ModelBuilder modelBuilder)
    {
        ConfigureProfiles(modelBuilder.Entity<UserProfile>());
        ConfigurePosts(modelBuilder.Entity<Post>());
        ConfigureComments(modelBuilder.Entity<Comment>());
        ConfigureLikes(modelBuilder.Entity<Like>());
        ConfigureFollows(modelBuilder.Entity<Follow>());
    }

    private static void ConfigureProfiles(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("Profiles");
        builder.HasKey(x => x.UserId);

        builder.Property(x => x.FullName).HasMaxLength(200);
        builder.Property(x => x.AvatarUrl).HasMaxLength(1000);
        builder.Property(x => x.Bio).HasMaxLength(1000);
        builder.Property(x => x.ClassName).HasMaxLength(100);
        builder.Property(x => x.Major).HasMaxLength(150);
        builder.Property(x => x.Interests).HasMaxLength(2000);
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2");
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
    }

    private static void ConfigurePosts(EntityTypeBuilder<Post> builder)
    {
        builder.ToTable("Posts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.Content).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.ImageUrl).HasMaxLength(1000);
        builder.Property(x => x.Hashtags).HasMaxLength(1000);
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2");
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");

        builder.HasIndex(x => new { x.AuthorId, x.CreatedAt }).IsDescending(false, true);
        builder.HasIndex(x => new { x.Status, x.CreatedAt }).IsDescending(false, true);

        builder.HasOne(x => x.Author)
            .WithMany(x => x.Posts)
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureComments(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("Comments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.Content).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2");
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");

        builder.HasIndex(x => new { x.PostId, x.CreatedAt }).IsDescending(false, true);

        builder.HasOne(x => x.Post)
            .WithMany(x => x.Comments)
            .HasForeignKey(x => x.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Author)
            .WithMany(x => x.Comments)
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureLikes(EntityTypeBuilder<Like> builder)
    {
        builder.ToTable("Likes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2");

        builder.HasIndex(x => new { x.PostId, x.UserId }).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.CreatedAt }).IsDescending(false, true);

        builder.HasOne(x => x.Post)
            .WithMany(x => x.Likes)
            .HasForeignKey(x => x.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany(x => x.Likes)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureFollows(EntityTypeBuilder<Follow> builder)
    {
        builder.ToTable("Follows", tableBuilder =>
            tableBuilder.HasCheckConstraint("CK_Follows_Follower_Following", "[FollowerId] <> [FollowingId]"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2");

        builder.HasIndex(x => new { x.FollowerId, x.FollowingId }).IsUnique();
        builder.HasIndex(x => x.FollowingId);

        builder.HasOne(x => x.Follower)
            .WithMany(x => x.Following)
            .HasForeignKey(x => x.FollowerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Following)
            .WithMany(x => x.Followers)
            .HasForeignKey(x => x.FollowingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
