using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InfotecsTest.Domain;

public class Result
{
    [Key]
    [ForeignKey("ValueData")]
    public string Name { get; set; } = string.Empty;
    public int deltaDate { get; set; }
    public DateTime minDateTime { get; set; }
    public decimal AverageExecutionTime { get; set; }
    public decimal AverageValue { get; set; }
    public decimal MedianValue { get; set; }
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; }
}
