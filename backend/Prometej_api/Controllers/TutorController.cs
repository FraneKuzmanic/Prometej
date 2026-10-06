using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Prometej_api.Tutor;
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

            var answer = await _tutorClient.Ask(model);

            return answer is null ? StatusCode(StatusCodes.Status503ServiceUnavailable) : Ok(answer);
        }
    }
}
