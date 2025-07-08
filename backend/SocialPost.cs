public class SocialPost
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Achievement { get; set; }
    public string? ImageUrl { get; set; }
    public string? Tag { get; set; }
    public bool Pinned { get; set; }
    public List<SocialLike> Likes { get; set; } = new();
    public List<SocialComment> Comments { get; set; } = new();
} 