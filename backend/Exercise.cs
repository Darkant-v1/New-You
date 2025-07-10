public class Exercise
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MuscleGroup { get; set; } = string.Empty;
    public string Equipment { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public int Sets { get; set; }
    public int Repetitions { get; set; }
    public int RestTimeSeconds { get; set; }
    public string Instructions { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int TimeLimitMinutes { get; set; }
    public string Notes { get; set; } = string.Empty;
    public double Weight { get; set; }
    public string Day { get; set; } = string.Empty;
    public string VideoUrl { get; set; } = string.Empty;
} 