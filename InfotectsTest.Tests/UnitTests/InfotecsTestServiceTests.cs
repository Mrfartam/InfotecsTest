using InfotecsTest.DBInfrastructure;
using InfotecsTest.Domain;
using InfotecsTest.Models;
using InfotecsTest.Services;
using InfotecsTest.Tests.IntegrationTests;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Globalization;

namespace InfotectsTest.Tests.UnitTests;

public class InfotecsTestServiceUnitTests : IClassFixture<TestApiFactory>, IDisposable
{
    private readonly TestApiFactory _factory;
    private readonly IServiceScope _scope;
    private readonly InfotecsTestDBContext _context;
    private readonly InfotecsTestService _service;

    public InfotecsTestServiceUnitTests(TestApiFactory factory)
    {
        _factory = factory;

        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<InfotecsTestDBContext>();

        _context.Values.ExecuteDelete();
        _context.Results.ExecuteDelete();

        _service = new InfotecsTestService(_context);
    }


    /// <summary>
    /// Тесты для метода ProcessAndSaveFileAsync
    /// </summary>
    // Метод для создания мок-объекта IFormFile
    private IFormFile CreateMockFormFile(string content, string fileName)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        var file = new Mock<IFormFile>();

        file.Setup(f => f.FileName).Returns(fileName);
        file.Setup(f => f.Length).Returns(stream.Length);
        file.Setup(f => f.OpenReadStream()).Returns(() =>
        {
            stream.Position = 0;
            return stream;
        });

        return file.Object;
    }

    // Тест на успешную обработку и сохранение файла
    [Fact]
    public async Task ProcessAndSaveFileAsync_ValidFile_ReturnIsSuccessTrue()
    {
        string validDate = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-ddTHH-mm-ss.ffffZ");

        string csvContent = "Date;ExecutionTime;Value\n"
            + $"{validDate};100;20.0\n"
            + $"{validDate};150;25.0\n"
            + $"{validDate};200;30.0\n";
        string fileName = "data.csv";

        var file = CreateMockFormFile(csvContent, fileName);

        var result = await _service.ProcessAndSaveFileAsync(file);

        Assert.True(result.IsSuccess);
        Assert.Equal("Файл успешно сохранён.", result.ErrorMessage);

        // Тесты на сохранение данных в базу
        var savedValues = await _context.Values.Where(v => v.Name == fileName).ToListAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(3, savedValues.Count);
        Assert.All(savedValues, v => Assert.Equal(fileName, v.Name));

        // Тесты на сохранение результатов в базу
        var savedResult = await _context.Results.FirstOrDefaultAsync(r => r.Name == fileName, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(savedResult);

        // Тест на корректность вычисленных интегральных результатов
        Assert.Equal(150, savedResult.AverageExecutionTime);
        Assert.Equal(25.0, savedResult.AverageValue);
        Assert.Equal(25.0, savedResult.MedianValue);
        Assert.Equal(20.0, savedResult.MinValue);
        Assert.Equal(30.0, savedResult.MaxValue);
    }

    // Тест на обработку файла с неверным расширением
    [Fact]
    public async Task ProcessAndSaveFileAsync_WrongExtension_ReturnErrors()
    {
        var file = CreateMockFormFile("TXT file", "data.txt");

        var result = await _service.ProcessAndSaveFileAsync(file);

        Assert.False(result.IsSuccess);
        Assert.Equal("Неверный формат файла. Ожидается CSV.", result.ErrorMessage);
    }

    // Тесты на обработку файла с неверным содержимым
    [Theory]
    [InlineData("", "Файл пустой или отсутствует")]
    [InlineData("Date;ExecutionTime;Value\n", "Количество строк должно быть от 1 до 10000.")]
    [InlineData("Date;ExecutionTime;Value\n1999-12-31T23-59-59.0000Z;100;200", "Дата не может быть позже текущей и раньше 01.01.2000.")]
    [InlineData("Date;ExecutionTime;Value\n2026-01-01T00-00-00.0000Z;-100;200", "Время выполнения не может быть меньше 0.")]
    [InlineData("Date;ExecutionTime;Value\n2026-01-01T00-00-00.0000Z;100;-200", "Значение показателя не может быть меньше 0.")]
    public async Task ProcessAndSaveFileAsync_InvalidData_ReturnErrors(string csvContent, string expectedErrorMessage)
    {
        var file = CreateMockFormFile(csvContent, "data.csv");

        var result = await _service.ProcessAndSaveFileAsync(file);

        Assert.False(result.IsSuccess);
        Assert.Equal(expectedErrorMessage, result.ErrorMessage);
    }

    /// <summary>
    /// Тесты для метода GetResultsByFiltersAsync
    /// </summary>
    // Метод для заполнения базы тестовыми данными
    private void SeedTestResults()
    {
        _context.Results.AddRange(
            new Result
            {
                Name = "first.csv",
                DeltaDate = 3600,
                StartDateTime = DateTime.ParseExact("2026-01-01T00-00-00.0000Z",
                    "yyyy-MM-ddTHH-mm-ss.FFFFZ",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal),
                AverageExecutionTime = 100,
                AverageValue = 20.0,
                MedianValue = 20.0,
                MinValue = 10.0,
                MaxValue = 30.0
            },
            new Result
            {
                Name = "second.csv",
                DeltaDate = 7200,
                StartDateTime = DateTime.ParseExact("2026-02-01T00-00-00.0000Z",
                    "yyyy-MM-ddTHH-mm-ss.FFFFZ",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal),
                AverageExecutionTime = 150,
                AverageValue = 25.0,
                MedianValue = 25.0,
                MinValue = 15.0,
                MaxValue = 35.0
            },
            new Result
            {
                Name = "third.csv",
                DeltaDate = 10800,
                StartDateTime = DateTime.ParseExact("2026-03-01T00-00-00.0000Z",
                    "yyyy-MM-ddTHH-mm-ss.FFFFZ",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal),
                AverageExecutionTime = 200,
                AverageValue = 30.0,
                MedianValue = 30.0,
                MinValue = 20.0,
                MaxValue = 40.0
            })
        ;
        _context.SaveChanges();
    }

    // Тест на фильтрацию результатов по дате начала записи
    [Theory]
    [InlineData("2026-01-01T00-00-00.0000Z", "2026-12-31T23-59-59.9999Z", 3)]
    [InlineData("2026-01-01T00-00-00.0000Z", "2026-01-31T23-59-59.9999Z", 1)]
    [InlineData("2026-02-01T00-00-00.0000Z", "2026-03-01T23-59-59.9999Z", 2)]
    public async Task GetResultsByFiltersAsync_FilterByStartDateTime_ReturnsMatchingRecords(string minDate, string maxDate, int expectedCount)
    {
        SeedTestResults();
        var filter = new ResultFilterDTO
        {
            MinStartDateTime = DateTime.ParseExact(minDate, "yyyy-MM-ddTHH-mm-ss.FFFFZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal),
            MaxStartDateTime = DateTime.ParseExact(maxDate, "yyyy-MM-ddTHH-mm-ss.FFFFZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal)
        };

        var results = await _service.GetResultsByFiltersAsync(filter);

        Assert.Equal(expectedCount, results.Count);
        Assert.All(results, r => Assert.True(r.StartDateTime >= filter.MinStartDateTime && r.StartDateTime <= filter.MaxStartDateTime));
    }

    // Тест на фильтрацию результатов по имени файла
    [Fact]
    public async Task GetResultsByFiltersAsync_FilterByFileName_ReturnsMatchingRecords()
    {
        SeedTestResults();

        var filter = new ResultFilterDTO
        {
            FileName = "first.csv"
        };

        var result = await _service.GetResultsByFiltersAsync(filter);

        Assert.Single(result);
        Assert.Equal("first.csv", result.First().Name);
    }

    // Тест на фильтрацию результатов по имени файла, которого нет в базе
    [Fact]
    public async Task GetResultsByFiltersAsync_FilterByNonExistingFileName_ReturnsEmptyList()
    {
        SeedTestResults();
        var filter = new ResultFilterDTO
        {
            FileName = "non_existing.csv"
        };

        var result = await _service.GetResultsByFiltersAsync(filter);

        Assert.Empty(result);
    }

    // Тест на фильтрацию результатов по среднему времени выполнения
    [Theory]
    [InlineData(75, 125, 1)]
    [InlineData(150, 200, 2)]
    [InlineData(100, 200, 3)]
    public async Task GetResultsByFiltersAsync_FilterByAverageExecutionTime_ReturnsMatchingRecords(double minExecutionTime, double maxExecutionTime, int expectedCount)
    {
        SeedTestResults();
        var filter = new ResultFilterDTO
        {
            MinAverageExecutionTime = minExecutionTime,
            MaxAverageExecutionTime = maxExecutionTime
        };

        var results = await _service.GetResultsByFiltersAsync(filter);

        Assert.Equal(expectedCount, results.Count);
        Assert.All(results, r => Assert.True(r.AverageExecutionTime >= minExecutionTime && r.AverageExecutionTime <= maxExecutionTime));
    }

    // Тест на фильтрацию результатов по среднему значению показателя
    [Theory]
    [InlineData(10.0, 20.0, 1)]
    [InlineData(10.0, 25.0, 2)]
    [InlineData(20.0, 30.0, 3)]
    public async Task GetResultsByFiltersAsync_FilterByAverageValue_ReturnsMatchingRecords(double minAverageValue, double maxAverageValue, int expectedCount)
    {
        SeedTestResults();
        var filter = new ResultFilterDTO
        {
            MinAverageValue = minAverageValue,
            MaxAverageValue = maxAverageValue
        };

        var results = await _service.GetResultsByFiltersAsync(filter);

        Assert.Equal(expectedCount, results.Count);
        Assert.All(results, r => Assert.True(r.AverageValue >= minAverageValue && r.AverageValue <= maxAverageValue));
    }

    // Тест на проверку пагинации результатов
    [Fact]
    public async Task GetResultsByFiltersAsync_Pagination_ReturnsCorrectPage()
    {
        SeedTestResults();
        var filter = new ResultFilterDTO
        {
            PageNumber = 2,
            PageSize = 1
        };

        var results = await _service.GetResultsByFiltersAsync(filter);

        Assert.Single(results);
        Assert.Equal("second.csv", results.First().Name);
    }

    /// <summary>
    /// Тесты для метода GetLast10ValuesByFilenameAsync
    /// </summary>
    // Метод для заполнения базы тестовыми данными
    private void SeedTestValues(string fileName, int count)
    {
        for (int i = 1; i <= count; i++)
        {
            _context.Values.Add(new ValueData
            {
                Name = fileName,
                Date = DateTime.UtcNow.AddDays(-i),
                ExecutionTime = i * 10,
                Value = i * 5.0
            });
        }
        _context.SaveChanges();
    }

    // Тест на получение последних 10 значений по имени файла
    [Fact]
    public async Task GetLast10ValuesByFilenameAsync_ExistingFile_ReturnsLast10Values()
    {
        SeedTestValues("test.csv", 15);

        var last10Values = await _service.GetLast10ValuesByFilenameAsync("test.csv");

        Assert.Equal(10, last10Values.Count);
        Assert.Equal(1, last10Values.First().ExecutionTime / 10); // Последняя запись из тестовой: с датой -1 день назад, ExecutionTime = 10
        Assert.Equal(10, last10Values.Last().ExecutionTime / 10); // Аналогично, 10-ая запись из тестовой: с датой -10 дней назад, ExecutionTime = 100
    }

    // Тест на получение последних 10 значений по имени файла, которого нет в базе
    [Fact]
    public async Task GetLast10ValuesByFilenameAsync_NonExistingFile_ReturnsEmptyList()
    {
        var last10Values = await _service.GetLast10ValuesByFilenameAsync("non_existing.csv");

        Assert.Empty(last10Values);
    }

    /// <summary>
    /// Освобождение ресурсов после завершения тестов
    /// </summary>
    public void Dispose()
    {
        _scope?.Dispose();
        _context?.Dispose();
    }
}
