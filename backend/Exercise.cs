public class Exercise
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty; // Short description
    public string MuscleGroup { get; set; } = string.Empty; // e.g., Chest, Legs
    public string Equipment { get; set; } = string.Empty; // e.g., Dumbbell, None
    public string Difficulty { get; set; } = string.Empty; // e.g., Beginner, Intermediate, Advanced
    public int Sets { get; set; }
    public int Repetitions { get; set; }
    public int RestTimeSeconds { get; set; } // Rest time between sets (seconds)
    public string Instructions { get; set; } = string.Empty; // Step-by-step instructions
    public string ImageUrl { get; set; } = string.Empty; // Optional: link to an exercise image
    public int TimeLimitMinutes { get; set; } // Optional: time limit for the exercise
    public string Notes { get; set; } = string.Empty;
    public double Weight { get; set; } // in kg
    public string Day { get; set; } = string.Empty; // e.g. Monday, or date
    public string VideoUrl { get; set; } = string.Empty; // New: YouTube or other video link
} 