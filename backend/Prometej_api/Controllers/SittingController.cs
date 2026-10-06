using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Prometej_api.Auth;
using Prometej_core.Models.Requests.Sitting;
using Prometej_core.Services.Contracts;

namespace Prometej_api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SittingController : ControllerBase
    {
        private readonly ISittingService _sittingService;

        public SittingController(ISittingService sittingService)
        {
            _sittingService = sittingService;
        }

        [EnableRateLimiting(QuizController.EntryCodeLimit)]
        [HttpGet("info/{quizId}")]
        public IActionResult Info(int quizId, [FromQuery] int? code)
        {
            return Ok(_sittingService.Info(quizId, code, User.GetUserId()));
        }

        // 201 for a new sitting, 200 for the caller's running one.
        [EnableRateLimiting(QuizController.EntryCodeLimit)]
        [HttpPost("start/{quizId}")]
        public IActionResult Start(int quizId, [FromQuery] int? code)
        {
            var (sitting, created) = _sittingService.Start(quizId, code, User.GetUserId());

            return StatusCode(created ? 201 : 200, sitting);
        }

        [HttpPut("{id}/answer")]
        public IActionResult SaveAnswer(int id, SittingAnswerRequest request)
        {
            _sittingService.SaveAnswer(id, request, User.GetUserId());

            return NoContent();
        }

        [HttpPost("{id}/finish")]
        public IActionResult Finish(int id)
        {
            return Ok(_sittingService.Finish(id, User.GetUserId()));
        }
    }
}
