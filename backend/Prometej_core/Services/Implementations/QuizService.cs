using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Prometej_core.DataAccessLayer;
using Prometej_core.Exceptions;
using Prometej_core.Models.Base;
using Prometej_core.Models.efModels;
using Prometej_core.Models.Requests.Quiz;
using Prometej_core.Models.ViewModels;
using Prometej_core.Services.Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Prometej_core.Services.Implementations
{
    public class QuizService: IQuizService
    {
        private readonly IMapper _mapper;
        private readonly IRepository<Quiz> _quizRepository;
        private readonly IRepository<Question> _questionRepository;
        private readonly IRepository<QuizGame> _quizGameRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Answer> _answerRepository;
        public QuizService(IMapper mapper, IRepository<Quiz> quizRepository, IRepository<Question> questionRepository, IRepository<QuizGame> quizGameRepository, IRepository<User> userRepository, IRepository<Answer> answerRepository)
        {
            _answerRepository = answerRepository;
            _mapper = mapper;
            _quizRepository = quizRepository;
            _questionRepository = questionRepository;
            _quizGameRepository = quizGameRepository;
            _userRepository = userRepository;
        }
         
        public List<QuizBaseModel> getAllPublicQuizzes()
        {
            var quizes = _quizRepository.ReadAll().Include(q => q.Creator).Where(q => !q.IsPrivate).ToList();
            List<QuizBaseModel> quizBaseModels = _mapper.Map<List<QuizBaseModel>>(quizes);

            return quizBaseModels;
        }

        public List<QuizBaseModel> searchQuizzes(string search)
        {
            var quizes = _quizRepository.ReadAll().Include(q => q.Creator).Where(q => !q.IsPrivate).Where(q => q.Title.Contains(search) || search.Contains(q.Creator.FirstName) || search.Contains(q.Creator.LastName) || q.Creator.FirstName.Contains(search) || q.Creator.LastName.Contains(search)).ToList();
            List<QuizBaseModel> quizBaseModels = _mapper.Map<List<QuizBaseModel>>(quizes);

            return quizBaseModels;
        }

        public List<QuizBaseModel> getAllUserQuizzes(int id)
        {
            var quizes = _quizRepository.ReadAll().Include(q => q.Creator).Where(q => q.CreatorId == id).ToList();
            List<QuizBaseModel> quizBaseModels = _mapper.Map<List<QuizBaseModel>>(quizes);

            return quizBaseModels;
        }

        public QuizViewModel GetQuiz(int id, int? code, int? callerId, bool isAdmin)
        {
            var quiz = _quizRepository.ReadAll().Include(q => q.Creator).Include(q => q.Questions.Where(x => !x.IsRetired).OrderBy(x => x.Id)).FirstOrDefault(q => q.Id == id);

            // A private quiz opens for its creator, an admin, or whoever has its entry code.
            // Everyone else gets the same answer as for a quiz that does not exist.
            var mayOpen = quiz != null
                && (!quiz.IsPrivate || isAdmin || quiz.CreatorId == callerId || (code != null && quiz.EntryCode == code));
            if (!mayOpen)
            {
                throw new NotFoundException("Quiz not found");
            }

            var quizViewModel = _mapper.Map<QuizViewModel>(quiz);

            return quizViewModel;
        }

        public QuizViewModel GetQuizByCode(int quizCode)
        {
            var quiz = _quizRepository.ReadAll().Include(q => q.Creator).Include(q => q.Questions.Where(x => !x.IsRetired).OrderBy(x => x.Id)).FirstOrDefault(q => q.IsPrivate && q.EntryCode == quizCode);
            if (quiz == null)
            {
                throw new NotFoundException("Quiz not found");
            }

            var quizViewModel = _mapper.Map<QuizViewModel>(quiz);

            return quizViewModel;
        }

        public int Create(QuizCreateRequest quiz, List<QuestionCreateRequest> questions, int creatorId)
        {
            TrimAndCheck(questions);

            var quizEntity = _mapper.Map<Quiz>(quiz);
            quizEntity.Title = quiz.Title.Trim();
            quizEntity.CreatorId = creatorId;
            // Added through the quiz, so one save stores the quiz and its questions or neither.
            quizEntity.Questions = _mapper.Map<List<Question>>(questions);
            _quizRepository.Create(quizEntity);
            SaveWithEntryCode(quizEntity);

            return quizEntity.Id;
        }

        public void Update(QuizEditRequest quiz, List<QuestionEditRequest>? questions, int callerId, bool isAdmin)
        {
            // Retired questions are not loaded, so an update can neither change nor restore one:
            // its id is refused below like the id of another quiz's question.
            var quizEntity = _quizRepository.GetAll().Include(q => q.Questions.Where(x => !x.IsRetired)).FirstOrDefault(q => q.Id == quiz.Id);
            EnsureCreatorOrAdmin(quizEntity, callerId, isAdmin);
            TrimAndCheck(questions ?? []);

            quizEntity.Title = quiz.Title.Trim();
            quizEntity.IsPrivate = quiz.IsPrivate;

            if (questions != null)
            {
                RemoveQuestionsNotIn(quizEntity, questions);
            }

            foreach (var questionRequest in questions ?? [])
            {
                if (questionRequest.Id == 0)
                {
                    var questionEntity = _mapper.Map<Question>(questionRequest);
                    questionEntity.QuizId = quizEntity.Id;
                    _questionRepository.Create(questionEntity);
                }
                else
                {
                    // An id from another quiz would otherwise let a caller rewrite another Creator's questions.
                    var questionEntity = quizEntity.Questions.FirstOrDefault(q => q.Id == questionRequest.Id);
                    if (questionEntity == null)
                    {
                        throw new ForbiddenException("Question does not belong to this quiz");
                    }

                    _mapper.Map(questionRequest, questionEntity);
                }
            }

            // The repositories share one context, so the quiz and its questions save together or not at all.
            SaveWithEntryCode(quizEntity);
        }

        public void Delete(int id, int callerId, bool isAdmin)
        {
            var quizEntity = _quizRepository.ReadAll().FirstOrDefault(q => q.Id == id);
            EnsureCreatorOrAdmin(quizEntity, callerId, isAdmin);

            _quizRepository.Delete(id);
            _quizRepository.Save();
        }

        public QuizGameViewModel SubmitQuiz(QuizSubmitRequest request, int userId)
        {
            var quiz = _quizRepository.ReadAll().Include(q => q.Questions.Where(x => !x.IsRetired)).FirstOrDefault(q => q.Id == request.QuizId);
            if (quiz == null)
            {
                throw new NotFoundException("Quiz not found");
            }

            // A creator trying out their own quiz is a preview, not a result for the analytics.
            if (quiz.CreatorId == userId)
            {
                throw new ForbiddenException("A creator's play of their own quiz is not recorded");
            }

            var questions = quiz.Questions.ToDictionary(q => q.Id);
            var answeredIds = request.Answers.Select(a => a.QuestionId).ToHashSet();
            if (request.Answers.Count != questions.Count || !answeredIds.SetEquals(questions.Keys))
            {
                throw new BadRequestException("A submission must answer every question of the quiz exactly once");
            }

            // Only foreign keys are set below. The quiz and its questions were read untracked,
            // so assigning them as navigations would make EF insert them a second time.
            var answers = new List<Answer>();
            var score = 0;
            foreach (var answer in request.Answers)
            {
                var question = questions[answer.QuestionId];
                string[] options = [question.FirstAnswer, question.SecondAnswer, question.ThirdAnswer, question.FourthAnswer];
                if (answer.ChosenOption == question.CorrectOption)
                {
                    score++;
                }

                // Both are kept as text, so the play still reads right after the question is edited.
                answers.Add(new Answer
                {
                    QuestionId = question.Id,
                    AnswerText = options[answer.ChosenOption - 1],
                    CorrectAnswer = options[question.CorrectOption - 1],
                });
            }

            var user = _userRepository.ReadAll().FirstOrDefault(u => u.Id == userId);
            if (user == null)
            {
                throw new NotFoundException("User not found");
            }

            var quizGame = new QuizGame
            {
                QuizId = quiz.Id,
                UserId = user.Id,
                UserName = user.FirstName + " " + user.LastName,
                Score = score,
                DatePlayed = DateTime.UtcNow,
                Answers = answers,
            };
            _quizGameRepository.Create(quizGame);
            _quizGameRepository.Save();

            return _mapper.Map<QuizGameViewModel>(quizGame);
        }

        public List<QuizGameViewModel> GetQuizAnalytics(int quizId, int callerId, bool isAdmin)
        {
            var quiz = _quizRepository.ReadAll().FirstOrDefault(q => q.Id == quizId);
            EnsureCreatorOrAdmin(quiz, callerId, isAdmin);

            var quizGames = _quizGameRepository.ReadAll().Include(g => g.Answers).Where(g => g.QuizId == quizId).OrderByDescending(g => g.DatePlayed).ToList();

            return _mapper.Map<List<QuizGameViewModel>>(quizGames);
        }

        // A sent list is the whole set: a stored question missing from it is removed. One that
        // was answered in a quiz game is retired instead, so that game keeps its answers and
        // its score still counts the questions it was played with.
        private void RemoveQuestionsNotIn(Quiz quiz, List<QuestionEditRequest> questions)
        {
            var keptIds = questions.Select(q => q.Id).ToHashSet();
            var removed = quiz.Questions.Where(q => !keptIds.Contains(q.Id)).ToList();
            var removedIds = removed.Select(q => q.Id).ToList();
            var answeredIds = _answerRepository.ReadAll()
                .Where(a => removedIds.Contains(a.QuestionId))
                .Select(a => a.QuestionId).Distinct().ToHashSet();

            foreach (var question in removed)
            {
                if (answeredIds.Contains(question.Id))
                {
                    question.IsRetired = true;
                }
                else
                {
                    _questionRepository.Delete(question.Id);
                }
            }
        }

        // The shape of a question (nothing empty, nothing too long, a correct option of 1 to 4) is
        // checked on the request models. What is stored is the trimmed text, so two options that
        // differ only by spaces around them are the same option.
        private static void TrimAndCheck(IEnumerable<QuestionCreateRequest> questions)
        {
            foreach (var question in questions)
            {
                question.QuestionTitle = question.QuestionTitle.Trim();
                question.FirstAnswer = question.FirstAnswer.Trim();
                question.SecondAnswer = question.SecondAnswer.Trim();
                question.ThirdAnswer = question.ThirdAnswer.Trim();
                question.FourthAnswer = question.FourthAnswer.Trim();

                string[] options = [question.FirstAnswer, question.SecondAnswer, question.ThirdAnswer, question.FourthAnswer];
                if (options.Distinct().Count() != options.Length)
                {
                    throw new BadRequestException("A question's four options must all differ");
                }
            }
        }

        // A private quiz gets an entry code, a public one has none. The lookup makes a taken
        // code unlikely; the unique index is what refuses one, and then another is tried.
        private void SaveWithEntryCode(Quiz quiz)
        {
            if (!quiz.IsPrivate)
            {
                quiz.EntryCode = null;
            }

            var needsCode = quiz.IsPrivate && quiz.EntryCode == null;
            for (var attempt = 1; ; attempt++)
            {
                if (needsCode)
                {
                    quiz.EntryCode = NewEntryCode();
                }

                try
                {
                    _quizRepository.Save();
                    return;
                }
                catch (DbUpdateException ex) when (needsCode && attempt < 3
                    && ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
                {
                }
            }
        }

        private int NewEntryCode()
        {
            int code;
            do
            {
                code = RandomNumberGenerator.GetInt32(10000, 100000);
            }
            while (_quizRepository.ReadAll().Any(q => q.EntryCode == code));

            return code;
        }

        private static void EnsureCreatorOrAdmin([NotNull] Quiz? quiz, int callerId, bool isAdmin)
        {
            if (quiz == null)
            {
                throw new NotFoundException("Quiz not found");
            }

            if (!isAdmin && quiz.CreatorId != callerId)
            {
                throw new ForbiddenException("Only the quiz's creator or an admin can do this");
            }
        }

    }
}
