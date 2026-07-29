using System.ComponentModel.DataAnnotations;

namespace InfotecsTest.Domain;

public class ValueData
{
    public int Id { get; set; }
    [Required]
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal ExecutionTime { get; set; }
    public decimal Value { get; set; }
}
