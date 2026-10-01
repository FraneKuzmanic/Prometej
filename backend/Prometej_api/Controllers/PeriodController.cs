using Microsoft.AspNetCore.Mvc;
using Prometej_core.Models.Requests.Period;
using Prometej_core.Models.Requests.User;
using Prometej_core.Services.Contracts;
using Prometej_core.Services.Implementations;
using Prometej_persistance;

namespace Prometej_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PeriodController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IPeriodService _periodService;

        public PeriodController(DataContext context, IPeriodService periodService)
        {
            _context = context;
            _periodService = periodService;
        }

        [HttpGet("content/{id}")]
        public IActionResult GetPeriodContent(int id)
        {
            try
            {
                return StatusCode(201, _periodService.GetPeriodContent(id));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("content/search/{query}")]
        public IActionResult SearchPeriodContent(string query)
        {
            try
            {
                return StatusCode(201, _periodService.SearchPeriodContent(query));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("content")]
        public IActionResult EditPeriodContent(PeriodContentEditRequest model)
        {
            try
            {
                return StatusCode(201, _periodService.UpdatePeriodContent(model));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

    }
}
