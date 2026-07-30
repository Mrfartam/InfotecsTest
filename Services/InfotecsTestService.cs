using CsvHelper.Configuration;
using InfotecsTest.DBInfrastructure;
using InfotecsTest.Domain;
using InfotecsTest.Models;
using InfotecsTest.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net.Sockets;

namespace InfotecsTest.Services;

public class InfotecsTestService: IInfotecsTestService
{
    private readonly InfotecsTestDBContext _context;

    public InfotecsTestService(InfotecsTestDBContext context)
    {
        _context = context;
    }

    public async Task<CVSUploadResultDTO> ProcessAndSaveFileAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return new CVSUploadResultDTO { IsSuccess = false, ErrorMessage = "Файл пустой или отсутствует" };

        if(!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return new CVSUploadResultDTO { IsSuccess = false, ErrorMessage = "Неверный формат файла. Ожидается CSV." };

        var records = new List<ValueData>();

        try
        {
            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);
            using var csv = new CsvHelper.CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";"
            });

            csv.Context.RegisterClassMap<ValueDTO>();

            await foreach (var record in csv.GetRecordsAsync<ValueData>())
            {
                record.Name = file.FileName;
                if (record.Date < new DateTime(2000, 1, 1) || record.Date > DateTime.UtcNow)
                    return new CVSUploadResultDTO { IsSuccess = false, ErrorMessage = "Дата не может быть позже текущей и раньше 01.01.2000." };
                if (record.ExecutionTime < 0)
                    return new CVSUploadResultDTO { IsSuccess = false, ErrorMessage = "Время выполнения не может быть меньше 0." };
                if (record.Value < 0)
                    return new CVSUploadResultDTO { IsSuccess = false, ErrorMessage = "Значение показателя не может быть меньше 0." };

                records.Add(record);
            }

            if (records.Count < 1 || records.Count > 10000)
                return new CVSUploadResultDTO { IsSuccess = false, ErrorMessage = "Количество строк должно быть от 1 до 10000." };

            await _context.Values.Where(v => v.Name == file.FileName).ExecuteDeleteAsync();

            _context.Values.AddRange(records);
            await _context.SaveChangesAsync();
            await CalculateIntegralResultsAsync(records);

            return new CVSUploadResultDTO { IsSuccess = true, ErrorMessage = "Файл успешно сохранён." };
        }
        catch (Exception ex)
        {
            switch (ex)
            {
                case InvalidOperationException:
                    return new CVSUploadResultDTO { IsSuccess = false, ErrorMessage = "Не удалось подключиться к базе данных." };
                case CsvHelper.TypeConversion.TypeConverterException:
                    return new CVSUploadResultDTO { IsSuccess = false, ErrorMessage = "Значения должны соответствовать своим типам, отсутствие одного из значений в записи недопустимо." };
                default:
                    return new CVSUploadResultDTO { IsSuccess = false, ErrorMessage = $"Ошибка при обработке файла: {ex.Message}" };
            }
        }
    }
    public async Task CalculateIntegralResultsAsync(List<ValueData> records)
    {
        var result = new Result
        {
            Name = records.First().Name,
            deltaDate = (int)(records.Max(r => r.Date) - records.Min(r => r.Date)).TotalSeconds,
            minDateTime = records.Min(r => r.Date),
            AverageExecutionTime = records.Average(r => r.ExecutionTime),
            AverageValue = records.Average(r => r.Value),
            MedianValue = records.OrderBy(r => r.Value).ElementAt(records.Count / 2).Value,
            MinValue = records.Min(r => r.Value),
            MaxValue = records.Max(r => r.Value)
        };

        var existingResult = await _context.Results.FirstOrDefaultAsync(r => r.Name == result.Name);
        if (existingResult != null)
            _context.Entry(existingResult).CurrentValues.SetValues(result);
        else
            _context.Results.Add(result);

        await _context.SaveChangesAsync();
    }
    public async Task<List<Result>> GetResultsByFiltersAsync(ResultFilterDTO filters)
    {
        var query = _context.Results.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filters.FileName))
            query = query.Where(r => r.Name.Contains(filters.FileName));
        if (filters.MinFirstRecordTime.HasValue)
            query = query.Where(r => r.minDateTime >= filters.MinFirstRecordTime.Value);
        if (filters.MaxFirstRecordTime.HasValue)
            query = query.Where(r => r.minDateTime <= filters.MaxFirstRecordTime.Value);
        if (filters.MinAverageValue.HasValue)
            query = query.Where(r => r.AverageValue >= filters.MinAverageValue.Value);
        if (filters.MaxAverageValue.HasValue)
            query = query.Where(r => r.AverageValue <= filters.MaxAverageValue.Value);
        if (filters.MinAverageExecutionTime.HasValue)
            query = query.Where(r => r.AverageExecutionTime >= filters.MinAverageExecutionTime.Value);
        if (filters.MaxAverageExecutionTime.HasValue)
            query = query.Where(r => r.AverageExecutionTime <= filters.MaxAverageExecutionTime.Value);

        query = query
            .Skip((filters.PageNumber - 1) * filters.PageSize)
            .Take(filters.PageSize);

        return await query.ToListAsync();
    }
    public async Task<List<ValueData>> GetLast10ValuesByFilenameAsync(string filename)
    {
        return await _context.Values.Where(v => v.Name == filename)
            .OrderByDescending(v => v.Date)
            .Take(10)
            .ToListAsync();
    }
}
