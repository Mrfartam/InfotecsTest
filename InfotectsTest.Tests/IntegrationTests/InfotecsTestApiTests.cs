using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace InfotecsTest.Tests.IntegrationTests;
public class InfotecsTestApiTests : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client;

    public InfotecsTestApiTests(TestApiFactory factory)
    {
        _client = factory.CreateClient();
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
    public async Task UploadFile_ValidFile_Returns200Ok()
    {
        string csv = "Date;ExecutionTime;Value\n2020-01-01T12-00-00.0000Z;100;20.0\n";
        var httpContent = CreateCsvHttpContent(csv, "api_test.csv");

        var response = await _client.PostAsync("/api/upload", httpContent, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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
    // Тест на получение результатов с фильтром
    [Fact]
    public async Task GetResults_WithFilter_Returns200Ok()
    {
        var response = await _client.GetAsync("/api/results", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Тест GET /api/last10values
    /// </summary>
    // Тест на получение последних 10 значений по имени файла
    [Fact]
    public async Task GetLast10Values_ValidFilename_Returns200Ok()
    {
        string csv = "Date;ExecutionTime;Value\n2020-01-01T12-00-00.0000Z;100;20.0\n";
        var httpContent = CreateCsvHttpContent(csv, "api_test.csv");

        await _client.PostAsync("/api/upload", httpContent, TestContext.Current.CancellationToken);
        var response = await _client.GetAsync("/api/last10values?filename=api_test.csv", TestContext.Current.CancellationToken);

        //Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            // Это выведет текст исключения и стек в вывод теста
            Assert.Fail($"Сервер вернул status {response.StatusCode}. Ответ сервера: {errorContent}");
        }
    }

    // Тест на получение последних 10 значений с пустым именем файла
    [Fact]
    public async Task GetLast10Values_EmptyFilename_Returns400BadRequest()
    {
        var response = await _client.GetAsync("/api/last10values?filename=", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}