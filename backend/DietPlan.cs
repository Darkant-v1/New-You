public class DietPlan
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public int Calories { get; set; }
    public string Macros { get; set; } = string.Empty; // e.g. "Protein: 100g, Carbs: 200g, Fat: 50g"
    public string Notes { get; set; } = string.Empty;
} 