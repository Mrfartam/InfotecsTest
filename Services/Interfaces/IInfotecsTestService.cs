namespace InfotecsTest.Services.Interfaces;
using InfotecsTest.Domain;
using InfotecsTest.Models;

public interface IInfotecsTestService
{
    Task<CVSUploadResultDTO> ProcessAndSaveFileAsync(IFormFile file);
    Task CalculateIntegralResultsAsync(List<ValueData> records);
    Task<List<Result>> GetResultsByFiltersAsync(ResultFilterDTO filters);
    Task<List<ValueData>> GetLast10ValuesByFilenameAsync(string filename);
}
