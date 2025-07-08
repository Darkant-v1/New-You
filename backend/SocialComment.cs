public class SocialComment
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public SocialPost? Post { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int? ParentCommentId { get; set; } // For threading
    public SocialComment? ParentComment { get; set; }
    public List<SocialComment> Replies { get; set; } = new();
} 