public class DietPlan
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public int Calories { get; set; }
    public string Macros { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class DietLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public int Calories { get; set; }
    public DateTime Date { get; set; }
} 