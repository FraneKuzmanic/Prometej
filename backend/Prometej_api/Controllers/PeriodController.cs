using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prometej_core.Auth;
using Prometej_core.Models.Requests.Period;
using Prometej_core.Services.Contracts;
using System.ComponentModel.DataAnnotations;

namespace Prometej_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PeriodController : ControllerBase
    {
        private readonly IPeriodService _periodService;

        public PeriodController(IPeriodService periodService)
        {
            _periodService = periodService;
        }

        [HttpGet("content/{id}")]
        public IActionResult GetPeriodContent(int id)
        {
            return Ok(_periodService.GetPeriodContent(id));
        }

        [HttpGet("content/search")]
        public IActionResult SearchPeriodContent([FromQuery, StringLength(100)] string? query)
        {
            return Ok(_periodService.SearchPeriodContent(query));
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpPost("content")]
        public IActionResult EditPeriodContent(PeriodContentEditRequest model)
        {
            return Ok(_periodService.UpdatePeriodContent(model));
        }
    }
}
