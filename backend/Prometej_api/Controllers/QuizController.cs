using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prometej_api.Auth;
using Prometej_core.Auth;
using Prometej_core.Models.Dtos;
using Prometej_core.Models.Requests.Quiz;
using Prometej_core.Services.Contracts;
using System.ComponentModel.DataAnnotations;

namespace Prometej_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuizController : ControllerBase
    {
        private readonly IQuizService _quizService;

        public QuizController(IQuizService quizService)
        {
            _quizService = quizService;
        }

        [HttpGet("getAll")]
        public IActionResult GetAllQuizzes()
        {
            return Ok(_quizService.getAllPublicQuizzes());
        }

        [HttpGet("search")]
        public IActionResult SearchQuizzes([FromQuery, StringLength(100)] string? query)
        {
            return Ok(_quizService.searchQuizzes(query));
        }

        [Authorize(Roles = Roles.TeacherOrAdmin)]
        [HttpGet("getAllUserQuizzes/{id}")]
        public IActionResult GetAllUserQuizzes(int id)
        {
            // The list includes private quizzes with their entry codes, so it is for their Creator only.
            if (id != User.GetUserId() && !User.IsAdmin())
            {
                return Forbid();
            }

            return Ok(_quizService.getAllUserQuizzes(id));
        }

        [HttpGet("get/{id}")]
        public IActionResult GetQuiz(int id, [FromQuery] int? code)
        {
            return Ok(_quizService.GetQuiz(id, code, User.GetUserIdOrNull(), User.IsAdmin()));
        }

        [HttpGet("getByCode/{quizCode}")]
        public IActionResult GetQuizByCode(int quizCode)
        {
            return Ok(_quizService.GetQuizByCode(quizCode));
        }

        [Authorize(Roles = Roles.TeacherOrAdmin)]
        [HttpPost("create")]
        public IActionResult CreateQuiz(QuizCreateDto quizDto)
        {
            return StatusCode(201, _quizService.Create(quizDto.Quiz, quizDto.Questions, User.GetUserId()));
        }

        [Authorize(Roles = Roles.TeacherOrAdmin)]
        [HttpPut("update")]
        public IActionResult UpdateQuiz(QuizEditDto quizDto)
        {
            _quizService.Update(quizDto.Quiz, quizDto.Questions, User.GetUserId(), User.IsAdmin());

            return NoContent();
        }

        [Authorize(Roles = Roles.TeacherOrAdmin)]
        [HttpDelete("delete/{id}")]
        public IActionResult DeleteQuiz(int id)
        {
            _quizService.Delete(id, User.GetUserId(), User.IsAdmin());

            return NoContent();
        }

        [Authorize]
        [HttpPost("submit")]
        public IActionResult SubmitQuiz(QuizSubmitRequest request)
        {
            return StatusCode(201, _quizService.SubmitQuiz(request, User.GetUserId()));
        }

        [Authorize(Roles = Roles.TeacherOrAdmin)]
        [HttpGet("getAnalytics/{id}")]
        public IActionResult GetQuizAnalytics(int id)
        {
            return Ok(_quizService.GetQuizAnalytics(id, User.GetUserId(), User.IsAdmin()));
        }
    }
}
