namespace InfotecsTest.Models;

public class ResultFilterDTO
{
    public string? FileName { get; set; }

    public DateTime? MinFirstRecordTime { get; set; }
    public DateTime? MaxFirstRecordTime { get; set; }

    public decimal? MinAverageValue { get; set; }
    public decimal? MaxAverageValue { get; set; }

    public decimal? MinAverageExecutionTime { get; set; }
    public decimal? MaxAverageExecutionTime { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
