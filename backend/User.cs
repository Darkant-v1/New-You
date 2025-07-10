using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class User
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string Username { get; set; } = string.Empty;
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    [Required]
    public string Role { get; set; } = "User";
    public string FullName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DietPlan? DietPlan { get; set; }
    public List<Exercise> Exercises { get; set; } = new();
    public List<SocialPost> SocialPosts { get; set; } = new();
    public List<SocialComment> SocialComments { get; set; } = new();
    public List<SocialLike> SocialLikes { get; set; } = new();
    public string AvatarUrl { get; set; } = string.Empty;
} 