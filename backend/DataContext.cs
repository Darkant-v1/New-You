using Microsoft.EntityFrameworkCore;

public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<DietPlan> DietPlans { get; set; }
    public DbSet<Exercise> Exercises { get; set; }
    public DbSet<SocialPost> SocialPosts { get; set; }
    public DbSet<SocialComment> SocialComments { get; set; }
    public DbSet<SocialLike> SocialLikes { get; set; }
    public DbSet<Message> Messages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // SocialPost relationships
        modelBuilder.Entity<SocialPost>()
            .HasMany(p => p.Comments)
            .WithOne(c => c.Post)
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SocialPost>()
            .HasMany(p => p.Likes)
            .WithOne(l => l.Post)
            .HasForeignKey(l => l.PostId)
            .OnDelete(DeleteBehavior.Cascade);
        // SocialComment threading
        modelBuilder.Entity<SocialComment>()
            .HasMany(c => c.Replies)
            .WithOne(c => c.ParentComment)
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
} 