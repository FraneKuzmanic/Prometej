using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Prometej_api.Auth;
using Prometej_core.Models.Requests.Discussion;
using Prometej_core.Services.Contracts;
using System.ComponentModel.DataAnnotations;

namespace Prometej_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DiscussionController : ControllerBase
    {
        // The name of the rate limiting policy for writing a topic or a reply.
        public const string PostingLimit = "posting";

        private readonly IDiscussionService _discussionService;

        public DiscussionController(IDiscussionService discussionService)
        {
            _discussionService = discussionService;
        }

        [HttpGet("period/{periodId}")]
        public IActionResult GetTopics(int periodId, [FromQuery, Range(1, 100000)] int page = 1)
        {
            return Ok(_discussionService.GetTopics(periodId, page));
        }

        [HttpGet("topic/{id}")]
        public IActionResult GetTopic(int id)
        {
            return Ok(_discussionService.GetTopic(id, User.GetUserIdOrNull(), User.IsAdmin()));
        }

        [Authorize]
        [EnableRateLimiting(PostingLimit)]
        [HttpPost("period/{periodId}")]
        public IActionResult CreateTopic(int periodId, TopicCreateRequest model)
        {
            return StatusCode(201, _discussionService.CreateTopic(periodId, model, User.GetUserId()));
        }

        [Authorize]
        [EnableRateLimiting(PostingLimit)]
        [HttpPost("topic/{id}/reply")]
        public IActionResult CreateReply(int id, ReplyCreateRequest model)
        {
            return StatusCode(201, _discussionService.CreateReply(id, model, User.GetUserId()));
        }

        [Authorize]
        [HttpDelete("topic/{id}")]
        public IActionResult DeleteTopic(int id)
        {
            _discussionService.DeleteTopic(id, User.GetUserId(), User.IsAdmin());

            return NoContent();
        }

        [Authorize]
        [HttpDelete("reply/{id}")]
        public IActionResult DeleteReply(int id)
        {
            _discussionService.DeleteReply(id, User.GetUserId(), User.IsAdmin());

            return NoContent();
        }
    }
}
