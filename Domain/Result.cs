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
    public double AverageExecutionTime { get; set; }
    public double AverageValue { get; set; }
    public double MedianValue { get; set; }
    public double MinValue { get; set; }
    public double MaxValue { get; set; }
}
