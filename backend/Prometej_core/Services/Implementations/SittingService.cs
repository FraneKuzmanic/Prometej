using System.Linq.Expressions;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Prometej_core.DataAccessLayer;
using Prometej_core.Exceptions;
using Prometej_core.Models.efModels;
using Prometej_core.Models.Requests.Sitting;
using Prometej_core.Models.ViewModels;
using Prometej_core.Services.Contracts;

namespace Prometej_core.Services.Implementations
{
    // A sitting is a play the server keeps: it hands out the questions without their answers,
    // stores each answer as it is given and ends the sitting by its own clock. There is no
    // background job, so a sitting whose time ran out is ended by the next request that
    // reads it.
    public class SittingService : ISittingService
    {
        private readonly IRepository<Sitting> _sittingRepository;
        private readonly IRepository<SittingAnswer> _sittingAnswerRepository;
        private readonly IRepository<Quiz> _quizRepository;
        private readonly IRepository<Question> _questionRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<QuizGame> _quizGameRepository;

        public SittingService(IRepository<Sitting> sittingRepository, IRepository<SittingAnswer> sittingAnswerRepository, IRepository<Quiz> quizRepository, IRepository<Question> questionRepository, IRepository<User> userRepository, IRepository<QuizGame> quizGameRepository)
        {
            _quizGameRepository = quizGameRepository;
            _sittingRepository = sittingRepository;
            _sittingAnswerRepository = sittingAnswerRepository;
            _quizRepository = quizRepository;
            _questionRepository = questionRepository;
            _userRepository = userRepository;
        }

        public SittingInfoViewModel Info(int quizId, int? code, int callerId)
        {
            var quiz = FindSittable(quizId, code);
            EndExpired(s => s.QuizId == quizId && s.UserId == callerId);

            var questions = QuestionsOf(quizId);
            var sittings = _sittingRepository.ReadAll().Where(s => s.QuizId == quizId && s.UserId == callerId);
            var lastEndedId = quiz.IsTest
                ? sittings.Where(s => s.FinishedAt != null).OrderByDescending(s => s.FinishedAt).Select(s => (int?)s.Id).FirstOrDefault()
                : null;

            return new SittingInfoViewModel
            {
                QuizId = quiz.Id,
                Title = quiz.Title,
                IsTest = quiz.IsTest,
                QuestionCount = questions.Count,
                MaxScore = questions.Sum(QuizScoring.PointsOf),
                TimeLimitMinutes = quiz.TimeLimitMinutes,
                ClosesAt = quiz.IsTest ? quiz.ClosesAt : null,
                IsClosed = quiz.IsTest && Quiz.IsClosed(quiz.ClosesAt),
                IsOwn = quiz.CreatorId == callerId,
                Running = sittings.Any(s => s.FinishedAt == null),
                Result = lastEndedId == null ? null : ResultOf(lastEndedId.Value),
            };
        }

        public (SittingViewModel Sitting, bool Created) Start(int quizId, int? code, int callerId)
        {
            var quiz = FindSittable(quizId, code);
            // A creator's result would be in their own analytics, and they have seen the answers.
            if (quiz.CreatorId == callerId)
            {
                throw new ForbiddenException("A creator does not sit their own quiz");
            }

            if (!EndExpired(s => s.QuizId == quizId && s.UserId == callerId))
            {
                throw new ConflictException("The sitting has just ended");
            }

            var runningId = RunningId(quizId, callerId);
            if (runningId != null)
            {
                return (ToViewModel(runningId.Value), false);
            }

            if (quiz.IsTest)
            {
                if (_sittingRepository.ReadAll().Any(s => s.QuizId == quizId && s.UserId == callerId))
                {
                    throw new ConflictException("A test is sat once");
                }

                if (Quiz.IsClosed(quiz.ClosesAt))
                {
                    throw new ConflictException("The test is closed");
                }
            }

            var now = DateTime.UtcNow;
            var sitting = new Sitting
            {
                QuizId = quizId,
                UserId = callerId,
                StartedAt = now,
                TimeLimitEndsAt = quiz.TimeLimitMinutes == null ? null : now.AddMinutes(quiz.TimeLimitMinutes.Value),
                // The lists of a matching and an ordering question are shuffled here, once, so
                // the numbers the user sends back say nothing about which is right.
                Answers = QuestionsOf(quizId).Select((question, i) => new SittingAnswer
                {
                    QuestionId = question.Id,
                    Position = i,
                    Shown = question.Type switch
                    {
                        QuestionTypes.Matching => Shuffle(QuizScoring.RightOptionsOf(question).Count),
                        QuestionTypes.Ordering => Shuffle(question.Content!.Items!.Count),
                        _ => null,
                    },
                }).ToList(),
            };
            _sittingRepository.Create(sitting);
            try
            {
                _sittingRepository.Save();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                // The same start arrived twice at once and the other request stored it.
                return (ToViewModel(RunningId(quizId, callerId)!.Value), false);
            }

            return (ToViewModel(sitting.Id), true);
        }

        public void SaveAnswer(int sittingId, SittingAnswerRequest request, int callerId)
        {
            EnsureOwn(sittingId, callerId);
            if (!EndExpired(s => s.Id == sittingId))
            {
                throw new ConflictException("The sitting has ended");
            }

            // Read after the lines above, which may have ended it.
            var stored = _sittingAnswerRepository.GetAll()
                .Include(a => a.Question)
                .Where(a => a.SittingId == sittingId && a.Sitting.FinishedAt == null)
                .FirstOrDefault(a => a.QuestionId == request.QuestionId);
            if (stored == null)
            {
                if (_sittingRepository.ReadAll().Any(s => s.Id == sittingId && s.FinishedAt == null))
                {
                    throw new NotFoundException("The sitting has no such question");
                }

                // No grace: the clock is the server's, and an answer that arrives late is not taken.
                throw new ConflictException("The sitting has ended");
            }

            EnsureShape(stored.Question, request.Given);
            // A new array: the column is compared by reference.
            stored.Given = [.. request.Given];
            try
            {
                _sittingAnswerRepository.Save();
            }
            catch (DbUpdateConcurrencyException)
            {
                // The sitting ended between the read and the save, and its rows went with it.
                throw new ConflictException("The sitting has ended");
            }
        }

        public SittingResultViewModel Finish(int sittingId, int callerId)
        {
            EnsureOwn(sittingId, callerId);
            if (EndExpired(s => s.Id == sittingId))
            {
                // Nothing happens if the line above, or an earlier request, has ended it.
                End(sittingId, SittingOutcomes.Submitted, DateTime.UtcNow);
            }

            return ResultOf(sittingId);
        }

        // A test is sat once and counts, so only a sitting of a public quiz can be given up.
        public void Discard(int sittingId, int callerId)
        {
            EnsureOwn(sittingId, callerId);
            if (!EndExpired(s => s.Id == sittingId))
            {
                throw new ConflictException("The sitting has ended");
            }

            // Gone already if the same discard was sent twice at once.
            var sitting = _sittingRepository.ReadAll().Where(s => s.Id == sittingId)
                .Select(s => new { s.Quiz.IsTest, s.FinishedAt }).FirstOrDefault()
                ?? throw new NotFoundException("Sitting not found");
            if (sitting.IsTest)
            {
                throw new ConflictException("A sitting of a test cannot be discarded");
            }

            if (sitting.FinishedAt != null)
            {
                throw new ConflictException("The sitting has ended");
            }

            _sittingRepository.Delete(sittingId);
            try
            {
                _sittingRepository.Save();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException("The sitting has ended");
            }
        }

        // Lets a student sit a test again: the sitting goes, and its result with it. A closed
        // test still has to be reopened by its closing time before the student can start.
        public void Reset(int sittingId, int callerId, bool isAdmin)
        {
            var sitting = _sittingRepository.GetAll().Include(s => s.Quiz).FirstOrDefault(s => s.Id == sittingId);
            if (sitting == null)
            {
                throw new NotFoundException("Sitting not found");
            }

            if (!isAdmin && sitting.Quiz.CreatorId != callerId)
            {
                throw new ForbiddenException("Only the quiz's creator or an admin can do this");
            }

            // A sitting of a public quiz is its user's own practice; there is nothing to allow again.
            if (!sitting.Quiz.IsTest)
            {
                throw new ConflictException("Only a sitting of a test can be reset");
            }

            if (sitting.QuizGameId != null)
            {
                _quizGameRepository.Delete(sitting.QuizGameId);
            }

            _sittingRepository.Delete(sitting.Id);
            try
            {
                _sittingRepository.Save();
            }
            catch (DbUpdateConcurrencyException)
            {
                // The student handed it in, or its time ran out, between the read and the delete.
                throw new ConflictException("The sitting has just changed");
            }
        }

        public bool EndExpired(Expression<Func<Sitting, bool>> which)
        {
            // The deadline depends on the quiz as well, so it is worked out here and not in SQL.
            var running = _sittingRepository.ReadAll().Where(which).Where(s => s.FinishedAt == null)
                .Select(s => new { s.Id, s.TimeLimitEndsAt, s.Quiz.IsTest, s.Quiz.ClosesAt })
                .ToList();

            var now = DateTime.UtcNow;
            foreach (var sitting in running)
            {
                var endsAt = EndsAt(sitting.TimeLimitEndsAt, sitting.IsTest ? sitting.ClosesAt : null);
                // Dated at its deadline: the request that notices may come days later.
                if (endsAt <= now && !End(sitting.Id, SittingOutcomes.Expired, endsAt.Value))
                {
                    return false;
                }
            }

            return true;
        }

        // A sitting ends at its time limit or when its test closes, whichever comes first.
        private static DateTime? EndsAt(DateTime? timeLimitEndsAt, DateTime? closesAt) =>
            timeLimitEndsAt == null ? closesAt : closesAt == null ? timeLimitEndsAt
                : timeLimitEndsAt < closesAt ? timeLimitEndsAt : closesAt;

        // Writes the sitting's quiz game from what was saved, with a row for every point,
        // answered or not, and marks the sitting as ended: all in one save. False when another
        // request ended it at the same moment. The context then still holds what was not
        // saved, so the request must write nothing more.
        private bool End(int sittingId, string outcome, DateTime finishedAt)
        {
            var sitting = _sittingRepository.GetAll().Include(s => s.Answers).FirstOrDefault(s => s.Id == sittingId);
            if (sitting == null || sitting.FinishedAt != null)
            {
                return true;
            }

            var saved = sitting.Answers.ToDictionary(a => a.QuestionId);
            var (answers, score) = QuizScoring.Score(
                QuestionsOf(sitting.QuizId), question => StoredNumbers(question, saved.GetValueOrDefault(question.Id)));
            var user = _userRepository.ReadAll().First(u => u.Id == sitting.UserId);

            // Only foreign keys are set: the quiz and the user were read untracked.
            sitting.QuizGame = new QuizGame
            {
                QuizId = sitting.QuizId,
                UserId = sitting.UserId,
                UserName = user.FirstName + " " + user.LastName,
                Score = score,
                DatePlayed = finishedAt,
                Answers = answers,
            };
            sitting.FinishedAt = finishedAt;
            sitting.Outcome = outcome;
            // The quiz game's rows are the record from here on.
            sitting.Answers.Clear();
            try
            {
                _sittingRepository.Save();
            }
            catch (DbUpdateConcurrencyException)
            {
                return false;
            }
            catch (Exception ex) when (PostgresErrors.IsDeadlock(ex))
            {
                // An edit of the quiz's questions was deleting this sitting at the same moment.
                return false;
            }

            return true;
        }

        // What was given for a question, turned from the numbering the sitting showed into the
        // stored one. Null for a point left empty, and for a question that was never answered.
        private static IReadOnlyList<int?> StoredNumbers(Question question, SittingAnswer? saved)
        {
            var given = saved?.Given ?? new int[QuizScoring.PointsOf(question)];

            return given.Select(number => number == 0 ? (int?)null : saved!.Shown == null ? number : saved.Shown[number - 1]).ToList();
        }

        // What `given` may hold depends on the question: a number for each of its points, none
        // beyond the list it chooses from, and no option or item used twice.
        private static void EnsureShape(Question question, int[] given)
        {
            var choices = question.Type switch
            {
                QuestionTypes.Matching => QuizScoring.RightOptionsOf(question).Count,
                QuestionTypes.Ordering => question.Content!.Items!.Count,
                _ => 4,
            };
            var chosen = given.Where(number => number != 0).ToList();
            if (given.Length != QuizScoring.PointsOf(question) || given.Any(number => number < 0 || number > choices)
                || chosen.Distinct().Count() != chosen.Count)
            {
                throw new BadRequestException("The answer does not fit the question");
            }
        }

        // The numbers 1 to count in a random order that is never the stored one: an ordering
        // question shown in its stored order would show its answer.
        private static int[] Shuffle(int count)
        {
            var numbers = Enumerable.Range(1, count).ToArray();
            do
            {
                RandomNumberGenerator.Shuffle<int>(numbers);
            }
            while (count > 1 && numbers.SequenceEqual(Enumerable.Range(1, count)));

            return numbers;
        }

        // A test is sat by whoever has its entry code, and a quiz the public list shows by
        // anyone, as a mock: its answers are only withheld here, since the quiz itself can be
        // read. Everything else, a private practice quiz included, gets the same answer as a
        // quiz that does not exist.
        private Quiz FindSittable(int quizId, int? code)
        {
            var quiz = _quizRepository.ReadAll().FirstOrDefault(q => q.Id == quizId);
            // A quiz stored before quizzes had to have questions can be made a test, and has
            // nothing to sit.
            var sittable = quiz != null && (quiz.IsTest
                ? code != null && quiz.EntryCode == code
                    && _questionRepository.ReadAll().Any(q => q.QuizId == quizId && !q.IsRetired)
                : _quizRepository.ReadAll().Where(Quiz.IsListed).Any(q => q.Id == quizId));
            if (!sittable)
            {
                throw new NotFoundException("Quiz not found");
            }

            return quiz!;
        }

        // Someone else's sitting gets the same answer as one that does not exist.
        private void EnsureOwn(int sittingId, int callerId)
        {
            if (!_sittingRepository.ReadAll().Any(s => s.Id == sittingId && s.UserId == callerId))
            {
                throw new NotFoundException("Sitting not found");
            }
        }

        private int? RunningId(int quizId, int userId) =>
            _sittingRepository.ReadAll().Where(s => s.QuizId == quizId && s.UserId == userId && s.FinishedAt == null)
                .Select(s => (int?)s.Id).FirstOrDefault();

        // The questions a sitting asks, in the quiz's order.
        private List<Question> QuestionsOf(int quizId) =>
            _questionRepository.ReadAll().Where(q => q.QuizId == quizId && !q.IsRetired)
                .OrderBy(q => q.Position).ThenBy(q => q.Id).ToList();

        private SittingResultViewModel ResultOf(int sittingId)
        {
            var result = _sittingRepository.ReadAll().Where(s => s.Id == sittingId && s.QuizGame != null)
                .Select(s => new
                {
                    GameId = s.QuizGame!.Id,
                    s.QuizGame.Score,
                    MaxScore = s.QuizGame.Answers.Count,
                    s.Outcome,
                    s.Quiz.IsTest,
                    s.Quiz.ClosesAt,
                })
                .FirstOrDefault() ?? throw new NotFoundException("Sitting not found");

            return new SittingResultViewModel
            {
                GameId = result.GameId,
                Score = result.Score,
                MaxScore = result.MaxScore,
                Outcome = result.Outcome!,
                ReviewAvailable = !result.IsTest || Quiz.IsClosed(result.ClosesAt),
            };
        }

        private SittingViewModel ToViewModel(int sittingId)
        {
            var sitting = _sittingRepository.ReadAll()
                .Include(s => s.Quiz)
                .Include(s => s.Answers.OrderBy(a => a.Position)).ThenInclude(a => a.Question).ThenInclude(q => q.SourceText)
                .First(s => s.Id == sittingId);

            return new SittingViewModel
            {
                Id = sitting.Id,
                QuizId = sitting.QuizId,
                QuizTitle = sitting.Quiz.Title,
                IsTest = sitting.Quiz.IsTest,
                StartedAt = sitting.StartedAt,
                EndsAt = EndsAt(sitting.TimeLimitEndsAt, sitting.Quiz.IsTest ? sitting.Quiz.ClosesAt : null),
                ServerNow = DateTime.UtcNow,
                Questions = sitting.Answers.Select(ToViewModel).ToList(),
                SourceTexts = sitting.Answers.Where(a => a.Question.SourceText != null).Select(a => a.Question.SourceText!)
                    .DistinctBy(text => text.Id)
                    .Select(text => new SourceTextViewModel { Id = text.Id, Caption = text.Caption, Body = text.Body })
                    .ToList(),
            };
        }

        // Member by member, and nothing that says which answer is right: no correct option,
        // no stored content, no hint, no explanation.
        private static SittingQuestionViewModel ToViewModel(SittingAnswer answer)
        {
            var question = answer.Question;
            var viewModel = new SittingQuestionViewModel
            {
                QuestionId = question.Id,
                QuestionTitle = question.QuestionTitle,
                Type = question.Type,
                SourceTextId = question.SourceTextId,
                Given = answer.Given,
            };

            switch (question.Type)
            {
                case QuestionTypes.Matching:
                    var rightOptions = QuizScoring.RightOptionsOf(question);
                    viewModel.Lefts = question.Content!.Pairs!.Select(pair => pair.Left).ToList();
                    viewModel.Rights = answer.Shown!.Select(number => rightOptions[number - 1]).ToList();
                    break;

                case QuestionTypes.Ordering:
                    viewModel.Items = answer.Shown!.Select(number => question.Content!.Items![number - 1]).ToList();
                    break;

                default:
                    viewModel.Options = [question.FirstAnswer!, question.SecondAnswer!, question.ThirdAnswer!, question.FourthAnswer!];
                    break;
            }

            return viewModel;
        }
    }
}
