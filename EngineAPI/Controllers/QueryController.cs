using EngineAPI.Service.Interface;
using Microsoft.AspNetCore.Mvc;

namespace EngineAPI.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class QueryController : ControllerBase
    {
        private readonly IQueryService _service;

        public QueryController(IQueryService service)
        {
            _service = service;
        }

        [HttpGet("query-processor")]
        public async Task<IActionResult> Get([FromQuery] Dictionary<string, string> queryParam)
        {
            var response = await _service.QueryAnnotator(queryParam);

            if (response.Success)
                return Ok(response);

            return new ObjectResult(response) { StatusCode = StatusCodes.Status500InternalServerError };
        }
    }
}
