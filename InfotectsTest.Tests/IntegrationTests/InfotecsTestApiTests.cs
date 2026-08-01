using InfotecsTest.DBInfrastructure;
using InfotecsTest.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace InfotecsTest.Tests.IntegrationTests;
public class InfotecsTestApiTests : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client;
    private readonly TestApiFactory _factory;

    public InfotecsTestApiTests(TestApiFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
    }

    // Вспомогательный метод отправки CSV как IFormFile
    private MultipartFormDataContent CreateCsvHttpContent(string csvContent, string fileName)
    {
        var content = new MultipartFormDataContent();
        var byteArray = Encoding.UTF8.GetBytes(csvContent);
        var byteContent = new ByteArrayContent(byteArray);

        byteContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(byteContent, "file", fileName);

        return content;
    }

    /// <summary>
    /// Тест POST /api/upload
    /// </summary>
    // Тест на успешную загрузку CSV файла
    [Fact]
    public async Task UploadFile_ValidFile_SavesDataToDb()
    {
        string csv = "Date;ExecutionTime;Value\n2020-01-01T12-00-00.0000Z;100;20.0\n";
        var httpContent = CreateCsvHttpContent(csv, "api_test.csv");

        var response = await _client.PostAsync("/api/upload", httpContent, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InfotecsTestDBContext>();

        var entityInDb = await dbContext.Values.FirstOrDefaultAsync(x => x.Name == "api_test.csv", cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(entityInDb);
        Assert.Equal(20.0, entityInDb.Value);
        Assert.Equal(100, entityInDb.ExecutionTime);
    }

    // Тест на загрузку CSV файла с неверным форматом (не CSV)
    [Fact]
    public async Task UploadFile_InvalidFileFormat_Returns400BadRequest()
    {
        var httpContent = new MultipartFormDataContent();
        var byteContent = new ByteArrayContent(Encoding.UTF8.GetBytes("hello"));
        httpContent.Add(byteContent, "file", "data.txt");

        var response = await _client.PostAsync("/api/upload", httpContent, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Тест GET /api/results
    /// </summary>
    // Тест на получение результатов без фильтрации
    [Fact]
    public async Task GetResults_WithoutFilter_Returns200Ok()
    {
        var response = await _client.GetAsync("/api/results", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Тест на получение результатов с фильтром по имени файла
    [Fact]
    public async Task GetResults_WithFilterByName_ReturnsExpectedData()
    {
        string csv = "Date;ExecutionTime;Value\n2020-01-01T12-00-00.0000Z;100;20.0\n2020-01-01T13-00-00.0000Z;200;40.0\n";
        var httpContent = CreateCsvHttpContent(csv, "api_test.csv");

        var uploadResponse = await _client.PostAsync("/api/upload", httpContent, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);

        var queryParams = "?FileName=api_test.csv";
        var response = await _client.GetAsync($"/api/results{queryParams}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var jsonString = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var results = JsonSerializer.Deserialize<List<Result>>(jsonString, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(results);
        Assert.Single(results);
        Assert.Equal("api_test.csv", results[0].Name);
    }

    // Тест на получение результатов с фильтром по минимальному среднему значению, когда данных нет
    [Fact]
    public async Task GetResults_WithFilterByMinAverageValueWithoutNeededData_ReturnsEmptyData()
    {
        string csv = "Date;ExecutionTime;Value\n2020-01-01T12-00-00.0000Z;100;20.0\n2020-01-01T13-00-00.0000Z;200;40.0\n";
        var httpContent = CreateCsvHttpContent(csv, "api_test.csv");

        var uploadResponse = await _client.PostAsync("/api/upload", httpContent, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);

        var queryParams = "?MinAverageValue=50.0";
        var response = await _client.GetAsync($"/api/results{queryParams}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var jsonString = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var results = JsonSerializer.Deserialize<List<Result>>(jsonString, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(results);
        Assert.Empty(results);
    }

    /// <summary>
    /// Тест GET /api/last10values
    /// </summary>
    // Тест на получение последних 10 значений по имени файла
    [Fact]
    public async Task GetLast10Values_ValidFilename_ReturnsExpectedData()
    {
        string csv = "Date;ExecutionTime;Value\n2020-01-01T12-00-00.0000Z;100;20.0\n";
        var httpContent = CreateCsvHttpContent(csv, "api_test.csv");

        await _client.PostAsync("/api/upload", httpContent, TestContext.Current.CancellationToken);
        var response = await _client.GetAsync("/api/last10values?filename=api_test.csv", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var jsonString = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        var resultList = JsonSerializer.Deserialize<List<ValueData>>(jsonString, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(resultList);
        Assert.Single(resultList);
        Assert.Equal("api_test.csv", resultList[0].Name);
        Assert.Equal(20.0, resultList[0].Value);
        Assert.Equal(100, resultList[0].ExecutionTime);
    }

    // Тест на получение последних 10 значений с пустым именем файла
    [Fact]
    public async Task GetLast10Values_EmptyFilename_Returns400BadRequest()
    {
        var response = await _client.GetAsync("/api/last10values?filename=", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}