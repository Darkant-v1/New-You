public class SocialLike
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public SocialPost? Post { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
} 