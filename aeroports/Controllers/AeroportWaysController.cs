using aeroports.Models;
using BLL.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace aeroports.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AeroportWaysController : ControllerBase
    {
        IMainService _mainService;
        public AeroportWaysController(IMainService mainService) {
            _mainService = mainService;
        }

        [HttpGet]
        [Route("{id}")]        
        public async Task<ActionResult<string>> Get(string id)
        {
            var dto = _mainService.Get(id);
            if (dto.isError) {
                return BadRequest(dto.Rezult);
            }

            return Ok(dto.Rezult);
        }

        [HttpPost]        
        public async Task<ActionResult<string>> Post([FromForm] AeroportWaysPostInputModel input)        
        {            
            var result = _mainService.Post(input.FromIATACode, input.ToIATACode);
            return result;
        }
    }
}
