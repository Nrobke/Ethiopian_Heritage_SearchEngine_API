using AngleSharp.Io;
using EngineAPI.Service.Interface;
using Microsoft.AspNetCore.Mvc;
using VDS.Common.Tries;

namespace EngineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConceptController : ControllerBase
    {
        private readonly IConceptService _service;

        public ConceptController(IConceptService service)
        {
            _service = service;
        }

        [HttpGet("add-concepts")]
        public async Task<IActionResult> Get()
        {
            var response =  await _service.AddConcept();

            if (response.Success)
                return Ok(response);

            return new ObjectResult(response) { StatusCode = StatusCodes.Status500InternalServerError };
        }
    }
}