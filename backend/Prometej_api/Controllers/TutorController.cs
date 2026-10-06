using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Prometej_api.Tutor;
using Prometej_core.Auth;
using Prometej_core.Models.Requests.Tutor;

namespace Prometej_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TutorController : ControllerBase
    {
        // The name of the rate limiting policy for a request that reaches the language model.
        public const string TutorLimit = "tutor";

        private readonly ITutorClient _tutorClient;

        public TutorController(ITutorClient tutorClient)
        {
            _tutorClient = tutorClient;
        }

        // The client shows the tutor only when this says so.
        [HttpGet]
        public async Task<IActionResult> GetStatus()
        {
            return Ok(new { available = await _tutorClient.IsAvailable() });
        }

        [EnableRateLimiting(TutorLimit)]
        [HttpPost("ask")]
        public async Task<IActionResult> Ask(TutorAskRequest model)
        {
            if (!_tutorClient.IsConfigured)
            {
                return NotFound();
            }

            var answer = await _tutorClient.Ask(model, HttpContext.RequestAborted);

            return answer is null ? StatusCode(StatusCodes.Status503ServiceUnavailable) : Ok(answer);
        }

        // Drafts of questions from one section of a Period's text. Nothing is stored: a
        // draft becomes a question only when its quiz is saved, through the quiz's own rules.
        [Authorize(Roles = Roles.TeacherOrAdmin)]
        [EnableRateLimiting(TutorLimit)]
        [HttpPost("drafts")]
        public async Task<IActionResult> Drafts(TutorDraftsRequest model)
        {
            if (!_tutorClient.IsConfigured)
            {
                return NotFound();
            }

            var drafts = await _tutorClient.Drafts(model, HttpContext.RequestAborted);

            return drafts is null ? StatusCode(StatusCodes.Status503ServiceUnavailable) : Ok(drafts);
        }
    }
}
