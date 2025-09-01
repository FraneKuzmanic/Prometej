using Microsoft.AspNetCore.Mvc;
using Prometej_core.Services.Contracts;
using Prometej_persistance;
using Microsoft.EntityFrameworkCore;
using Prometej_core.Models.efModels;
using Prometej_core.Models.Requests.Quiz;
using Prometej_core.Services.Implementations;
using Prometej_core.Models.Dtos;

namespace Prometej_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuizController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IQuizService _quizService;

        public QuizController(DataContext context, IQuizService quizService)
        {
            _context = context;
            _quizService = quizService;
        }

        [HttpGet("getAll")]
        public IActionResult GetAllQuizzes()
        {
            try
            {
                return StatusCode(201, _quizService.getAllPublicQuizzes());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("search/{query}")]
        public IActionResult SearchQuizzes(string query)
        {
            try
            {
                return StatusCode(201, _quizService.searchQuizzes(query));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("getAllUserQuizzes/{id}")]
        public IActionResult GetAllUserQuizzes(int id)
        {
            try
            {
                return StatusCode(201, _quizService.getAllUserQuizzes(id));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("get/{id}")]
        public IActionResult GetQuiz(int id)
        {
            try
            {
                return StatusCode(201, _quizService.GetQuiz(id));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("getByCode/{quizCode}")]
        public IActionResult GetQuizByCode(int quizCode)
        {
            try
            {
                return StatusCode(201, _quizService.GetQuizByCode(quizCode));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("create")]
        public IActionResult CreateQuiz(QuizCreateDto quizDto)
        {
            var quiz = quizDto.Quiz;
            var questions = quizDto.Questions;
            try
            {
                return StatusCode(201, _quizService.Create(quiz, questions));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("update")]
        public IActionResult UpdateQuiz(QuizEditDto quizDto)
        {
            var quiz = quizDto.Quiz;
            var questions = quizDto.Questions;
            try
            {
                _quizService.Update(quiz, questions);
                return StatusCode(201);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("delete/{id}")]
        public IActionResult DeleteQuiz(int id)
        {
            try
            {
                _quizService.Delete(id);
                return StatusCode(201);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


    }
}
