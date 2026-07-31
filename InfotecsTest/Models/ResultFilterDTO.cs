namespace InfotecsTest.Models;

public class ResultFilterDTO
{
    public string? FileName { get; set; }

    public DateTime? MinStartDateTime { get; set; }
    public DateTime? MaxStartDateTime { get; set; }

    public double? MinAverageValue { get; set; }
    public double? MaxAverageValue { get; set; }

    public double? MinAverageExecutionTime { get; set; }
    public double? MaxAverageExecutionTime { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
