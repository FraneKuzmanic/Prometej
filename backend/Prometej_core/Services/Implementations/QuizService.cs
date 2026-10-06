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
        private readonly IRepository<Period> _periodRepository;
        private readonly IRepository<SourceText> _sourceTextRepository;
        private readonly IRepository<Sitting> _sittingRepository;
        private readonly ISittingService _sittingService;
        public QuizService(IMapper mapper, IRepository<Quiz> quizRepository, IRepository<Question> questionRepository, IRepository<QuizGame> quizGameRepository, IRepository<User> userRepository, IRepository<Answer> answerRepository, IRepository<Period> periodRepository, IRepository<SourceText> sourceTextRepository, IRepository<Sitting> sittingRepository, ISittingService sittingService)
        {
            _sittingService = sittingService;
            _sittingRepository = sittingRepository;
            _sourceTextRepository = sourceTextRepository;
            _periodRepository = periodRepository;
            _answerRepository = answerRepository;
            _mapper = mapper;
            _quizRepository = quizRepository;
            _questionRepository = questionRepository;
            _quizGameRepository = quizGameRepository;
            _userRepository = userRepository;
        }
         
        // The public list: every listed Quiz, or those of one Period, or those a search finds.
        public List<QuizBaseModel> SearchQuizzes(string? search, int? periodId)
        {
            var quizzes = _quizRepository.ReadAll().Where(Quiz.IsListed);
            if (periodId != null)
            {
                quizzes = quizzes.Where(q => q.PeriodId == periodId);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                // PostgreSQL text cannot hold a NUL character, so no title has one and the query
                // could not even be sent.
                if (search.Contains('\0'))
                {
                    return [];
                }

                // The query is text to find, so the characters LIKE reads as wildcards are escaped.
                var pattern = "%" + search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
                // unaccent on both sides first: what ILIKE then compares is plain ASCII, so the
                // match ignores case and diacritics whatever the database's locale is.
                quizzes = quizzes
                    .Where(q => EF.Functions.ILike(EF.Functions.Unaccent(q.Title), EF.Functions.Unaccent(pattern), "\\")
                             || EF.Functions.ILike(EF.Functions.Unaccent(q.Creator.FirstName + " " + q.Creator.LastName), EF.Functions.Unaccent(pattern), "\\"));
            }

            // Curriculum order, as on the learning screen. The first key puts the Quizzes
            // without a Period last, whatever the database does with a null when it sorts.
            var listed = quizzes.Include(q => q.Creator).Include(q => q.Period)
                .OrderBy(q => q.PeriodId == null).ThenBy(q => q.Period!.SortOrder).ThenBy(q => q.Id)
                .ToList();
            var quizBaseModels = _mapper.Map<List<QuizBaseModel>>(listed);
            SetQuestionCounts(quizBaseModels);

            return quizBaseModels;
        }

        // A Creator's own list: Private Quizzes and their Entry Codes included.
        public List<CreatorQuizViewModel> GetMyQuizzes(int creatorId)
        {
            var quizes = _quizRepository.ReadAll().Include(q => q.Creator).Include(q => q.Period).Where(q => q.CreatorId == creatorId).OrderBy(q => q.Id).ToList();
            var quizGameCounts = _quizGameRepository.ReadAll().Where(g => g.Quiz.CreatorId == creatorId)
                .GroupBy(g => g.QuizId).Select(g => new { QuizId = g.Key, Count = g.Count() })
                .ToDictionary(g => g.QuizId, g => g.Count);

            var creatorQuizzes = _mapper.Map<List<CreatorQuizViewModel>>(quizes);
            foreach (var quiz in creatorQuizzes)
            {
                quiz.QuizGameCount = quizGameCounts.GetValueOrDefault(quiz.Id);
            }
            SetQuestionCounts(creatorQuizzes);

            return creatorQuizzes;
        }

        private void SetQuestionCounts(IReadOnlyCollection<QuizBaseModel> quizzes)
        {
            var quizIds = quizzes.Select(q => q.Id).ToList();
            var questionCounts = _questionRepository.ReadAll().Where(x => quizIds.Contains(x.QuizId) && !x.IsRetired)
                .GroupBy(x => x.QuizId).Select(g => new { QuizId = g.Key, Count = g.Count() })
                .ToDictionary(g => g.QuizId, g => g.Count);

            foreach (var quiz in quizzes)
            {
                quiz.QuestionCount = questionCounts.GetValueOrDefault(quiz.Id);
            }
        }

        public QuizViewModel GetQuiz(int id, int? code, int? callerId, bool isAdmin)
        {
            var quiz = _quizRepository.ReadAll().Include(q => q.Creator).Include(q => q.Period).Include(q => q.Questions.Where(x => !x.IsRetired).OrderBy(x => x.Position).ThenBy(x => x.Id)).ThenInclude(x => x.SourceText).FirstOrDefault(q => q.Id == id);

            // A private quiz opens for its creator, an admin, or whoever has its entry code.
            // Everyone else gets the same answer as for a quiz that does not exist.
            var mayOpen = quiz != null
                && (!quiz.IsPrivate || isAdmin || quiz.CreatorId == callerId || (code != null && quiz.EntryCode == code));
            if (!mayOpen)
            {
                throw new NotFoundException("Quiz not found");
            }

            return ToViewModel(quiz!, withQuestions: !quiz!.IsTest || isAdmin || quiz.CreatorId == callerId);
        }

        public QuizViewModel GetQuizByCode(int quizCode)
        {
            var quiz = _quizRepository.ReadAll().Include(q => q.Creator).Include(q => q.Period).Include(q => q.Questions.Where(x => !x.IsRetired).OrderBy(x => x.Position).ThenBy(x => x.Id)).ThenInclude(x => x.SourceText).FirstOrDefault(q => q.IsPrivate && q.EntryCode == quizCode);
            if (quiz == null)
            {
                throw new NotFoundException("Quiz not found");
            }

            // Nobody is known to be calling, so a test is its header to everyone here.
            return ToViewModel(quiz, withQuestions: !quiz.IsTest);
        }

        // The quiz with the source texts its questions are asked about, in the order they come up.
        // A test's questions carry their answers, so they leave the server here only for its
        // creator or an admin; a student gets them in a sitting, without the answers.
        private QuizViewModel ToViewModel(Quiz quiz, bool withQuestions)
        {
            var quizViewModel = _mapper.Map<QuizViewModel>(quiz);
            if (!withQuestions)
            {
                quizViewModel.Questions = [];
                return quizViewModel;
            }

            quizViewModel.SourceTexts = _mapper.Map<List<SourceTextViewModel>>(
                quiz.Questions.Where(q => q.SourceText != null).Select(q => q.SourceText).DistinctBy(s => s!.Id).ToList());

            return quizViewModel;
        }

        public int Create(QuizCreateRequest quiz, List<QuestionCreateRequest> questions, List<SourceTextRequest>? sourceTexts, int creatorId)
        {
            sourceTexts ??= [];
            TrimAndCheck(questions);
            TrimAndCheckSourceTexts(sourceTexts, questions);
            EnsurePeriodExists(quiz.PeriodId);
            EnsureTestIsPrivate(quiz.IsTest, quiz.IsPrivate);

            var quizEntity = _mapper.Map<Quiz>(quiz);
            quizEntity.Title = quiz.Title.Trim();
            quizEntity.ClosesAt = quiz.IsTest ? quiz.ClosesAt?.UtcDateTime : null;
            quizEntity.CreatorId = creatorId;
            // Added through the quiz, so one save stores the quiz, its questions and its source
            // texts, or none of them.
            quizEntity.Questions = _mapper.Map<List<Question>>(questions);
            quizEntity.SourceTexts = sourceTexts.Select(text => new SourceText { Caption = text.Caption, Body = text.Body }).ToList();
            for (var i = 0; i < quizEntity.Questions.Count; i++)
            {
                quizEntity.Questions[i].Position = i;
                quizEntity.Questions[i].Content = ContentOf(questions[i]);
                quizEntity.Questions[i].SourceText = SourceTextOf(questions[i], quizEntity.SourceTexts);
            }
            _quizRepository.Create(quizEntity);
            SaveWithEntryCode(quizEntity);

            return quizEntity.Id;
        }

        public void Update(QuizEditRequest quiz, List<QuestionEditRequest>? questions, List<SourceTextRequest>? sourceTexts, int callerId, bool isAdmin)
        {
            // Retired questions and source texts are not loaded, so an update can neither change
            // nor restore one: its id is refused below like the id of another quiz's.
            var quizEntity = _quizRepository.GetAll()
                .Include(q => q.Questions.Where(x => !x.IsRetired))
                .Include(q => q.SourceTexts.Where(s => !s.IsRetired))
                .FirstOrDefault(q => q.Id == quiz.Id);
            EnsureCreatorOrAdmin(quizEntity, callerId, isAdmin);
            // Without questions the source texts are left alone too, whatever was sent.
            sourceTexts = questions == null ? [] : sourceTexts ?? [];
            TrimAndCheck(questions ?? []);
            TrimAndCheckSourceTexts(sourceTexts, questions ?? []);
            EnsurePeriodExists(quiz.PeriodId);
            EnsureTestIsPrivate(quiz.IsTest, quiz.IsPrivate);

            var hasSittings = _sittingRepository.ReadAll().Any(s => s.QuizId == quizEntity.Id);
            // A result says how it was played, and a sitting was started under one set of rules.
            if (quiz.IsTest != quizEntity.IsTest
                && (hasSittings || _quizGameRepository.ReadAll().Any(g => g.QuizId == quizEntity.Id)))
            {
                throw new ConflictException("A quiz's mode cannot change once it has results");
            }

            // Every student sits the same test.
            if (quizEntity.IsTest && questions != null && hasSittings)
            {
                throw new ConflictException("A test's questions cannot change once it was started");
            }

            quizEntity.Title = quiz.Title.Trim();
            quizEntity.IsPrivate = quiz.IsPrivate;
            quizEntity.PeriodId = quiz.PeriodId;
            quizEntity.IsTest = quiz.IsTest;
            quizEntity.TimeLimitMinutes = quiz.TimeLimitMinutes;
            quizEntity.ClosesAt = quiz.IsTest ? quiz.ClosesAt?.UtcDateTime : null;

            List<SourceText> sourceTextEntities = [];
            if (questions != null)
            {
                RemoveQuestionsNotIn(quizEntity, questions);
                sourceTextEntities = StoreSourceTexts(quizEntity, sourceTexts);
            }

            // The place in the sent list is the question's place in the quiz.
            var position = 0;
            foreach (var questionRequest in questions ?? [])
            {
                Question? questionEntity;
                if (questionRequest.Id == 0)
                {
                    questionEntity = _mapper.Map<Question>(questionRequest);
                    questionEntity.QuizId = quizEntity.Id;
                    _questionRepository.Create(questionEntity);
                }
                else
                {
                    // An id from another quiz would otherwise let a caller rewrite another Creator's questions.
                    questionEntity = quizEntity.Questions.FirstOrDefault(q => q.Id == questionRequest.Id);
                    if (questionEntity == null)
                    {
                        throw new ForbiddenException("Question does not belong to this quiz");
                    }

                    // Its stored answers have the rows of the type it was played as.
                    if (questionEntity.Type != questionRequest.Type)
                    {
                        throw new BadRequestException("A question's type cannot be changed");
                    }

                    _mapper.Map(questionRequest, questionEntity);
                }

                questionEntity.Position = position++;
                questionEntity.Content = ContentOf(questionRequest);
                questionEntity.SourceText = SourceTextOf(questionRequest, sourceTextEntities);
                if (questionEntity.SourceText == null)
                {
                    questionEntity.SourceTextId = null;
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
            // A submit that repeats its key is the same play sent again. It gets the answer the
            // first one got, whatever has happened to the quiz since, so this comes first.
            if (request.SubmissionKey != null)
            {
                var submitted = FindSubmitted(request, userId);
                if (submitted != null)
                {
                    return submitted;
                }
            }

            var quiz = _quizRepository.ReadAll().Include(q => q.Questions.Where(x => !x.IsRetired)).FirstOrDefault(q => q.Id == request.QuizId);
            if (quiz == null)
            {
                throw new NotFoundException("Quiz not found");
            }

            // Its questions were never sent to be played this way.
            if (quiz.IsTest)
            {
                throw new ForbiddenException("A test is taken in a sitting");
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
            // The rows follow the quiz's order, whatever order the request names its questions in.
            var chosen = request.Answers.ToDictionary(a => a.QuestionId);
            var (answers, score) = QuizScoring.Score(
                quiz.Questions.OrderBy(q => q.Position).ThenBy(q => q.Id),
                question => Complete(question, chosen[question.Id]));

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
                SubmissionKey = request.SubmissionKey,
            };
            _quizGameRepository.Create(quizGame);
            try
            {
                _quizGameRepository.Save();
            }
            catch (DbUpdateException ex) when (request.SubmissionKey != null
                && ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                // The same play arrived twice at once and the other request stored it.
                return FindSubmitted(request, userId)!;
            }

            return _mapper.Map<QuizGameViewModel>(quizGame);
        }

        // The numbers a practice play gives for one question, which has to answer all of it.
        private static IReadOnlyList<int?> Complete(Question question, AnswerSubmitRequest answer)
        {
            switch (question.Type)
            {
                case QuestionTypes.Matching:
                    var matches = answer.Matches;
                    if (matches == null || matches.Count != QuizScoring.PointsOf(question) || matches.Distinct().Count() != matches.Count
                        || matches.Any(number => number < 1 || number > QuizScoring.RightOptionsOf(question).Count))
                    {
                        throw new BadRequestException("A matching question is answered with Matches: a different option number for each of its pairs");
                    }

                    return matches.Select(number => (int?)number).ToList();

                case QuestionTypes.Ordering:
                    var order = answer.Order;
                    if (order == null || !order.Order().SequenceEqual(Enumerable.Range(1, QuizScoring.PointsOf(question))))
                    {
                        throw new BadRequestException("An ordering question is answered with Order: the number of each of its items, once");
                    }

                    return order.Select(number => (int?)number).ToList();

                default:
                    if (answer.ChosenOption == null)
                    {
                        throw new BadRequestException("A choice question is answered with ChosenOption");
                    }

                    return [answer.ChosenOption];
            }
        }

        // The quiz game the caller already stored with this request's key, if there is one.
        private QuizGameViewModel? FindSubmitted(QuizSubmitRequest request, int userId)
        {
            var quizGame = _quizGameRepository.ReadAll()
                .Include(g => g.Answers.OrderBy(a => a.Position).ThenBy(a => a.QuestionId).ThenBy(a => a.Id))
                .FirstOrDefault(g => g.UserId == userId && g.SubmissionKey == request.SubmissionKey);
            if (quizGame == null)
            {
                return null;
            }

            // Answering with another quiz's game would show its score as this play's result.
            if (quizGame.QuizId != request.QuizId)
            {
                throw new ConflictException("This submission key was used for another quiz");
            }

            return _mapper.Map<QuizGameViewModel>(quizGame);
        }

        public MyQuizGamesViewModel GetMyGames(int userId)
        {
            _sittingService.EndExpired(s => s.UserId == userId);

            // Every game the caller played, also of a quiz that is private or no longer listed.
            var now = DateTime.UtcNow;
            var games = _quizGameRepository.ReadAll().Where(g => g.UserId == userId)
                .OrderByDescending(g => g.DatePlayed).ThenByDescending(g => g.Id)
                .Select(g => new PlayedQuizGameViewModel
                {
                    Id = g.Id,
                    QuizId = g.QuizId,
                    QuizTitle = g.Quiz.Title,
                    PeriodName = g.Quiz.Period!.Name,
                    Score = g.Score,
                    MaxScore = g.Answers.Count,
                    DatePlayed = g.DatePlayed,
                    PlayedAs = g.Quiz.IsTest ? PlayedAs.Test : g.Sitting != null ? PlayedAs.Mock : PlayedAs.Practice,
                    ReviewAvailable = !g.Quiz.IsTest || (g.Quiz.ClosesAt != null && g.Quiz.ClosesAt <= now),
                })
                .ToList();

            return new MyQuizGamesViewModel { Progress = ProgressByPeriod(games), Games = games };
        }

        // A Period's progress counts the quizzes the public list shows for it, and how many of
        // those the player has played. The percentage is the average of the player's best play
        // of each played quiz, so practising a quiz again never lowers it.
        private List<PeriodProgressViewModel> ProgressByPeriod(List<PlayedQuizGameViewModel> games)
        {
            var bestByQuiz = games.Where(g => g.MaxScore > 0).GroupBy(g => g.QuizId)
                .ToDictionary(g => g.Key, g => g.Max(game => (double)game.Score / game.MaxScore));
            var listedByPeriod = _quizRepository.ReadAll().Where(Quiz.IsListed).Where(q => q.PeriodId != null)
                .Select(q => new { q.Id, PeriodId = q.PeriodId!.Value })
                .ToLookup(q => q.PeriodId, q => q.Id);

            var progress = new List<PeriodProgressViewModel>();
            foreach (var period in _periodRepository.ReadAll().OrderBy(p => p.SortOrder))
            {
                var best = listedByPeriod[period.Id].Where(bestByQuiz.ContainsKey).Select(quizId => bestByQuiz[quizId]).ToList();
                if (best.Count == 0)
                {
                    continue;
                }

                progress.Add(new PeriodProgressViewModel
                {
                    PeriodId = period.Id,
                    PeriodName = period.Name,
                    QuizCount = listedByPeriod[period.Id].Count(),
                    PlayedCount = best.Count,
                    AverageBestPercent = (int)Math.Round(best.Average() * 100, MidpointRounding.AwayFromZero),
                });
            }

            return progress;
        }

        public QuizGameReviewViewModel GetQuizGame(int id, int callerId)
        {
            // A game is its player's alone. Someone else's gets the same answer as one that
            // does not exist. The answers come in the order the questions were played in.
            var quizGame = _quizGameRepository.ReadAll()
                .Include(g => g.Answers.OrderBy(a => a.Position).ThenBy(a => a.QuestionId).ThenBy(a => a.Id))
                .Include(g => g.Quiz).ThenInclude(q => q.Period)
                .Include(g => g.Sitting)
                .FirstOrDefault(g => g.Id == id && g.UserId == callerId);
            if (quizGame == null)
            {
                throw new NotFoundException("Quiz game not found");
            }

            // The answers of a test are the same for the students still sitting it.
            if (quizGame.Quiz.IsTest && !Quiz.IsClosed(quizGame.Quiz.ClosesAt))
            {
                throw new ConflictException("The review opens when the test is closed");
            }

            var review = _mapper.Map<QuizGameReviewViewModel>(quizGame);
            review.PlayedAs = PlayedAs.Of(quizGame.Quiz.IsTest, quizGame.Sitting != null);
            review.QuizIsListed = _quizRepository.ReadAll().Where(Quiz.IsListed).Any(q => q.Id == quizGame.QuizId);
            // The versions the answers point to, which may have been retired since.
            var sourceTextIds = quizGame.Answers.Where(a => a.SourceTextId != null).Select(a => a.SourceTextId!.Value).Distinct().ToList();
            if (sourceTextIds.Count > 0)
            {
                review.SourceTexts = _mapper.Map<List<SourceTextViewModel>>(
                    _sourceTextRepository.ReadAll().Where(s => sourceTextIds.Contains(s.Id)).OrderBy(s => s.Id).ToList());
            }

            return review;
        }

        public QuizAnalyticsViewModel GetQuizAnalytics(int quizId, int callerId, bool isAdmin)
        {
            var quiz = _quizRepository.ReadAll().FirstOrDefault(q => q.Id == quizId);
            EnsureCreatorOrAdmin(quiz, callerId, isAdmin);
            _sittingService.EndExpired(s => s.QuizId == quizId);

            var quizGames = _quizGameRepository.ReadAll()
                .Include(g => g.Answers.OrderBy(a => a.Position).ThenBy(a => a.QuestionId).ThenBy(a => a.Id))
                .Where(g => g.QuizId == quizId)
                .OrderByDescending(g => g.DatePlayed).ThenByDescending(g => g.Id)
                .ToList();

            return new QuizAnalyticsViewModel
            {
                QuizTitle = quiz.Title,
                Games = _mapper.Map<List<QuizGameViewModel>>(quizGames),
                Questions = QuestionReport(quizId, quizGames),
                Players = PlayerSummaries(quizGames),
            };
        }

        // A row for every question of the quiz, retired ones too: their answers are in the old
        // scores. An answer counts as it was played, right when its two texts are equal, and is
        // never read against the question as it is today.
        private List<QuestionReportViewModel> QuestionReport(int quizId, List<QuizGame> quizGames)
        {
            var answers = quizGames.SelectMany(g => g.Answers).ToLookup(a => a.QuestionId);
            var questions = _questionRepository.ReadAll().Where(q => q.QuizId == quizId).OrderBy(q => q.Position).ThenBy(q => q.Id)
                .Select(q => new { q.Id, q.QuestionTitle, q.Type, q.IsRetired, q.SourceTextId, SourceTextCaption = q.SourceText!.Caption })
                .ToList();

            var report = new List<QuestionReportViewModel>();
            foreach (var question in questions)
            {
                // The rows of a matching or an ordering question, by the item or the place they
                // were played for, in the order those first come up. Grouping a list keeps it.
                var lines = answers[question.Id].Where(a => a.Item != null || a.Place != null)
                    .GroupBy(a => a.Item ?? a.Place.ToString()!)
                    .Select(line =>
                    {
                        var wrong = MostChosenWrong(line);
                        return new QuestionLineViewModel
                        {
                            Label = line.Key,
                            AnswerCount = line.Count(),
                            CorrectCount = line.Count(a => a.AnswerText == a.CorrectAnswer),
                            MostChosenWrongAnswer = wrong?.Key,
                            MostChosenWrongCount = wrong?.Count() ?? 0,
                        };
                    }).ToList();
                var mostChosenWrong = lines.Count > 0 ? null : MostChosenWrong(answers[question.Id]);

                report.Add(new QuestionReportViewModel
                {
                    QuestionId = question.Id,
                    QuestionTitle = question.QuestionTitle,
                    Type = question.Type,
                    IsRetired = question.IsRetired,
                    SourceTextId = question.SourceTextId,
                    SourceTextCaption = question.SourceTextCaption,
                    AnswerCount = answers[question.Id].Count(),
                    CorrectCount = answers[question.Id].Count(a => a.AnswerText == a.CorrectAnswer),
                    MostChosenWrongAnswer = mostChosenWrong?.Key,
                    MostChosenWrongCount = mostChosenWrong?.Count() ?? 0,
                    Lines = lines,
                });
            }

            return report;
        }

        // Of two wrong answers chosen equally often, the one that sorts first. A point left
        // empty in a sitting is wrong, but nobody chose it.
        private static IGrouping<string, Answer>? MostChosenWrong(IEnumerable<Answer> answers) =>
            answers.Where(a => a.AnswerText != a.CorrectAnswer && a.AnswerText != "")
                .GroupBy(a => a.AnswerText)
                .OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal)
                .FirstOrDefault();

        // One row per player. Their best game is the one with the highest share of correct
        // answers, not the highest score: two plays of an edited quiz may differ in length.
        // Of two equally good games it is the earlier one.
        private static List<PlayerSummaryViewModel> PlayerSummaries(List<QuizGame> quizGames)
        {
            static double Share(QuizGame g) => g.Answers.Count == 0 ? 0 : (double)g.Score / g.Answers.Count;

            // The games are newest first and grouping a list keeps its order, so the players
            // come out by their last play, newest first, and so do each player's games.
            return quizGames.GroupBy(g => g.UserId).Select(group =>
            {
                var games = group.ToList();
                var last = games.First();
                var first = games.Last();
                var best = games.OrderByDescending(Share).ThenBy(g => g.DatePlayed).ThenBy(g => g.Id).First();

                return new PlayerSummaryViewModel
                {
                    UserId = group.Key,
                    UserName = last.UserName,
                    GameCount = games.Count,
                    FirstScore = first.Score,
                    FirstMaxScore = first.Answers.Count,
                    BestScore = best.Score,
                    BestMaxScore = best.Answers.Count,
                    LastPlayed = last.DatePlayed,
                };
            }).ToList();
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

        // The shape of a question (what its type carries, how many, how long) is checked on the
        // request models. What is stored is the trimmed text, so two options that differ only by
        // spaces around them are the same option, and a hint or an explore more text of nothing
        // but spaces is none. A seeded quiz does not pass the request models' checks, so a text
        // that is missing here is refused and not assumed.
        private static void TrimAndCheck(IEnumerable<QuestionCreateRequest> questions)
        {
            foreach (var question in questions)
            {
                question.QuestionTitle = question.QuestionTitle.Trim();
                question.HintText = TrimOrNull(question.HintText);
                question.ExploreMore = TrimOrNull(question.ExploreMore);

                switch (question.Type)
                {
                    case QuestionTypes.Choice:
                        question.FirstAnswer = TrimOrNull(question.FirstAnswer);
                        question.SecondAnswer = TrimOrNull(question.SecondAnswer);
                        question.ThirdAnswer = TrimOrNull(question.ThirdAnswer);
                        question.FourthAnswer = TrimOrNull(question.FourthAnswer);
                        if (question.CorrectOption is not (>= 1 and <= 4))
                        {
                            throw new BadRequestException("A choice question needs a correct option");
                        }

                        EnsureAllDiffer(
                            [question.FirstAnswer, question.SecondAnswer, question.ThirdAnswer, question.FourthAnswer],
                            "A question's four options must all differ");
                        break;

                    case QuestionTypes.Matching:
                        var pairs = question.Content?.Pairs ?? [];
                        foreach (var pair in pairs)
                        {
                            pair.Left = pair.Left?.Trim()!;
                            pair.Right = pair.Right?.Trim()!;
                        }

                        var extras = (question.Content?.Extras ?? []).Select(extra => extra?.Trim() ?? "").ToList();
                        EnsureAllDiffer(pairs.Select(pair => pair.Left), "A matching question's left items must all differ");
                        // An extra equal to a pair's right-hand text would make two options read the same.
                        EnsureAllDiffer(pairs.Select(pair => pair.Right).Concat(extras), "A matching question's right-hand options must all differ");
                        question.Content!.Extras = extras.Count > 0 ? extras : null;
                        break;

                    case QuestionTypes.Ordering:
                        var items = (question.Content?.Items ?? []).Select(item => item?.Trim() ?? "").ToList();
                        EnsureAllDiffer(items, "An ordering question's items must all differ");
                        question.Content!.Items = items;
                        break;

                    default:
                        throw new BadRequestException("The question type is not known");
                }
            }
        }

        // Also refuses a list with nothing in it and a text that is missing or empty.
        private static void EnsureAllDiffer(IEnumerable<string?> texts, string message)
        {
            var list = texts.ToList();
            if (list.Count == 0 || list.Any(string.IsNullOrEmpty) || list.Distinct().Count() != list.Count)
            {
                throw new BadRequestException(message);
            }
        }

        // A new object each time: the column is compared by reference.
        private static QuestionContent? ContentOf(QuestionCreateRequest question) => question.Type switch
        {
            QuestionTypes.Matching => new QuestionContent
            {
                Pairs = question.Content!.Pairs!.Select(pair => new MatchPair { Left = pair.Left, Right = pair.Right }).ToList(),
                Extras = question.Content.Extras,
            },
            QuestionTypes.Ordering => new QuestionContent { Items = question.Content!.Items },
            _ => null,
        };

        private static string? TrimOrNull(string? text) =>
            string.IsNullOrWhiteSpace(text) ? null : text.Trim();

        // A body keeps its line breaks, stored as "\n" whatever the browser sent. A source text
        // has one to ten questions, and they come one after another in the quiz.
        private static void TrimAndCheckSourceTexts(List<SourceTextRequest> sourceTexts, IEnumerable<QuestionCreateRequest> questions)
        {
            foreach (var sourceText in sourceTexts)
            {
                sourceText.Caption = (sourceText.Caption ?? "").Trim();
                sourceText.Body = (sourceText.Body ?? "").Trim().Replace("\r\n", "\n").Replace('\r', '\n');
                if (sourceText.Caption == "" || sourceText.Body == "")
                {
                    throw new BadRequestException("A source text needs a caption and a body");
                }
            }

            if (sourceTexts.Where(s => s.Id != 0).GroupBy(s => s.Id).Any(g => g.Count() > 1))
            {
                throw new BadRequestException("A source text must not be sent twice");
            }

            var questionCounts = new int[sourceTexts.Count];
            int? previous = null;
            foreach (var question in questions)
            {
                var number = question.SourceTextNo;
                if (number != null)
                {
                    if (number < 1 || number > sourceTexts.Count)
                    {
                        throw new BadRequestException("A question names a source text that was not sent");
                    }

                    // Counted before, and another question came in between.
                    if (number != previous && questionCounts[number.Value - 1] > 0)
                    {
                        throw new BadRequestException("The questions of a source text must be together");
                    }

                    questionCounts[number.Value - 1]++;
                }

                previous = number;
            }

            if (questionCounts.Any(count => count < 1 || count > 10))
            {
                throw new BadRequestException("A source text needs between one and ten questions");
            }
        }

        private static SourceText? SourceTextOf(QuestionCreateRequest question, List<SourceText> sourceTexts) =>
            question.SourceTextNo == null ? null : sourceTexts[question.SourceTextNo.Value - 1];

        // The sent list is the whole set, in the order the questions number it. A stored source
        // text that was shown in a quiz game is never changed or deleted: a changed one gets a
        // new row and the old one is retired, so that game's review still shows what was read.
        private List<SourceText> StoreSourceTexts(Quiz quiz, List<SourceTextRequest> sourceTexts)
        {
            var stored = quiz.SourceTexts.ToDictionary(s => s.Id);
            // An id from another quiz would otherwise let a caller rewrite another Creator's source text.
            if (sourceTexts.Any(s => s.Id != 0 && !stored.ContainsKey(s.Id)))
            {
                throw new ForbiddenException("Source text does not belong to this quiz");
            }

            var sent = sourceTexts.Where(s => s.Id != 0).ToDictionary(s => s.Id);
            var changedOrRemovedIds = stored.Values
                .Where(s => !sent.TryGetValue(s.Id, out var request) || request.Caption != s.Caption || request.Body != s.Body)
                .Select(s => s.Id).ToList();
            var playedIds = _answerRepository.ReadAll()
                .Where(a => a.SourceTextId != null && changedOrRemovedIds.Contains(a.SourceTextId.Value))
                .Select(a => a.SourceTextId!.Value).Distinct().ToHashSet();

            var entities = new List<SourceText>();
            foreach (var request in sourceTexts)
            {
                var entity = request.Id == 0 ? null : stored[request.Id];
                if (entity != null && playedIds.Contains(entity.Id))
                {
                    entity.IsRetired = true;
                    entity = null;
                }

                if (entity == null)
                {
                    entity = new SourceText { QuizId = quiz.Id, Caption = request.Caption, Body = request.Body };
                    _sourceTextRepository.Create(entity);
                }
                else
                {
                    entity.Caption = request.Caption;
                    entity.Body = request.Body;
                }

                entities.Add(entity);
            }

            foreach (var removed in stored.Values.Where(s => !sent.ContainsKey(s.Id)))
            {
                if (playedIds.Contains(removed.Id))
                {
                    removed.IsRetired = true;
                }
                else
                {
                    _sourceTextRepository.Delete(removed.Id);
                }
            }

            return entities;
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

        private static void EnsureTestIsPrivate(bool isTest, bool isPrivate)
        {
            if (isTest && !isPrivate)
            {
                throw new BadRequestException("A test is a private quiz");
            }
        }

        // Without this the foreign key would refuse the id, as a server error.
        private void EnsurePeriodExists(int? periodId)
        {
            if (periodId != null && !_periodRepository.ReadAll().Any(p => p.Id == periodId))
            {
                throw new BadRequestException("Period not found");
            }
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
