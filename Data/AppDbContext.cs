using BibleReader.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BibleReader.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    public DbSet<BibleVersion> BibleVersions => Set<BibleVersion>();
    public DbSet<BibleBook> BibleBooks => Set<BibleBook>();
    public DbSet<BibleChapter> BibleChapters => Set<BibleChapter>();
    public DbSet<BibleVerse> BibleVerses => Set<BibleVerse>();

    public DbSet<UserReadingPlan> UserReadingPlans => Set<UserReadingPlan>();
    public DbSet<ReadingPlanDay> ReadingPlanDays => Set<ReadingPlanDay>();
    public DbSet<ReadingPlanDayChapter> ReadingPlanDayChapters => Set<ReadingPlanDayChapter>();

    public DbSet<CommunityPost> CommunityPosts => Set<CommunityPost>();
    public DbSet<CommunityPostLike> CommunityPostLikes => Set<CommunityPostLike>();
    public DbSet<CommunityComment> CommunityComments => Set<CommunityComment>();
    public DbSet<CommunitySavedPost> CommunitySavedPosts => Set<CommunitySavedPost>();

    public DbSet<SupportMessage> SupportMessages => Set<SupportMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>()
            .HasIndex(x => x.Email)
            .IsUnique();

        modelBuilder.Entity<EmailVerificationToken>()
            .HasOne(x => x.AppUser)
            .WithMany()
            .HasForeignKey(x => x.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PasswordResetToken>()
            .HasOne(x => x.AppUser)
            .WithMany()
            .HasForeignKey(x => x.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BibleBook>()
            .HasIndex(x => new { x.BibleVersionId, x.Slug })
            .IsUnique();

        modelBuilder.Entity<BibleChapter>()
            .HasIndex(x => new { x.BibleBookId, x.ChapterNumber })
            .IsUnique();

        modelBuilder.Entity<BibleChapter>()
            .HasIndex(x => x.GlobalOrder);

        modelBuilder.Entity<UserReadingPlan>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ReadingPlanDay>()
            .HasIndex(x => new { x.UserReadingPlanId, x.CalendarDate })
            .IsUnique();

        modelBuilder.Entity<ReadingPlanDayChapter>()
            .HasIndex(x => new { x.ReadingPlanDayId, x.BibleChapterId })
            .IsUnique();

        modelBuilder.Entity<ReadingPlanDayChapter>()
            .HasOne(x => x.BibleChapter)
            .WithMany()
            .HasForeignKey(x => x.BibleChapterId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CommunityPost>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CommunityPostLike>()
            .HasIndex(x => new { x.PostId, x.UserId })
            .IsUnique();

        modelBuilder.Entity<CommunityPostLike>()
            .HasOne(x => x.Post)
            .WithMany(x => x.Likes)
            .HasForeignKey(x => x.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CommunityPostLike>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CommunityComment>()
            .HasOne(x => x.Post)
            .WithMany(x => x.Comments)
            .HasForeignKey(x => x.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CommunityComment>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CommunitySavedPost>()
            .HasIndex(x => new { x.PostId, x.UserId })
            .IsUnique();

        modelBuilder.Entity<CommunitySavedPost>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CommunitySavedPost>()
            .HasOne(x => x.Post)
            .WithMany()
            .HasForeignKey(x => x.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        foreach (var e in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var p in e.GetProperties())
            {
                if (p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?))
                    p.SetColumnType("datetime2");
            }
        }
    }
}
