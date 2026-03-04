using Microsoft.AspNetCore.Mvc;
using Transflo.Platform.Transformer.Core.DTOs;
using Transflo.Platform.Transformer.Core.Services;

namespace Transflo.Platform.Transformer.WebApi.Controllers;

[ApiController]
[Route("api/v1/json/parse")]
[Tags("JSON Parser")]
public class JsonParserController : ControllerBase
{
    private readonly IJsonParserService _service;

    public JsonParserController(IJsonParserService service)
    {
        _service = service;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ParseJson([FromBody] JsonParseRequest request)
    {
        var isValid = await _service.ValidateJsonAsync(request.JsonString);
        if (!isValid)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("Invalid JSON"));
        }

        var fields = await _service.ExtractFieldPathsAsync(request.JsonString, request.IncludeSampleValues);

        var response = new
        {
            IsValid = true,
            Fields = fields,
            TotalFields = fields.Count
        };

        return Ok(ApiResponse<object>.SuccessResponse(response));
    }
}
