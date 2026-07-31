using InfotecsTest.Services.Interfaces;
using InfotecsTest.Models;
using Microsoft.AspNetCore.Mvc;

namespace InfotecsTest.Controllers;

[ApiController]
[Route("api")]
public class InfotecsTestController: ControllerBase
{
    private readonly IInfotecsTestService _infotecsTestService;
    public InfotecsTestController(IInfotecsTestService infotecsTestService)
    {
        _infotecsTestService = infotecsTestService;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        var result = await _infotecsTestService.ProcessAndSaveFileAsync(file);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });
        return Ok(new { message = result.ErrorMessage });
    }

    [HttpGet("results")]
    public async Task<IActionResult> GetResults([FromQuery] ResultFilterDTO filter)
    {
        var results = await _infotecsTestService.GetResultsByFiltersAsync(filter);
        return Ok(results);
    }

    [HttpGet("last10values")]
    public async Task<IActionResult> GetLast10Values([FromQuery] string filename)
    {
        if(string.IsNullOrWhiteSpace(filename))
            return BadRequest("Имя файла не может быть пустым.");

        var values = await _infotecsTestService.GetLast10ValuesByFilenameAsync(filename);
        return Ok(values);
    }
}
