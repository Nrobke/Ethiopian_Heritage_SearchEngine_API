using EngineAPI.Service.Interface;
using Microsoft.AspNetCore.Mvc;

namespace EngineAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentService _service;

        public DocumentController(IDocumentService service)
        {
            _service = service;
        }


        [HttpGet("annotate-document")]
        public async Task<IActionResult> Get()
        {
            var response = await _service.DocumentAnnotator();

            if (response.Success)
                return Ok(response);

            return new ObjectResult(response) { StatusCode = StatusCodes.Status500InternalServerError };
        }
    }
}
