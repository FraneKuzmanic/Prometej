using AutoMapper;
using Microsoft.EntityFrameworkCore;
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
using System.Text;
using System.Threading.Tasks;

namespace Prometej_core.Services.Implementations
{
    public class QuizService: IQuizService
    {
        private readonly IMapper _mapper;
        private readonly IRepository<Quiz> _quizRepository;
        private readonly IRepository<Question> _questionRepository;
        public QuizService(IMapper mapper, IRepository<Quiz> quizRepository, IRepository<Question> questionRepository)
        {
            _mapper = mapper;
            _quizRepository = quizRepository;
            _questionRepository = questionRepository;
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
            var quiz = _quizRepository.ReadAll().Include(q => q.Creator).Include(q => q.Questions).FirstOrDefault(q => q.Id == id);

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
            var quiz = _quizRepository.ReadAll().Include(q => q.Creator).Include(q => q.Questions).FirstOrDefault(q => q.EntryCode == quizCode);
            if (quiz == null)
            {
                throw new NotFoundException("Quiz not found");
            }

            var quizViewModel = _mapper.Map<QuizViewModel>(quiz);

            return quizViewModel;
        }

        public int Create(QuizCreateRequest quiz, List<QuestionCreateRequest> questions, int creatorId)
        {
            var quizEntity = _mapper.Map<Quiz>(quiz);
            quizEntity.CreatorId = creatorId;
            _quizRepository.Create(quizEntity);
            _quizRepository.Save();

            // Map questions and set the QuizId
            foreach (var questionRequest in questions)
            {
                var questionEntity = _mapper.Map<Question>(questionRequest);
                questionEntity.QuizId = quizEntity.Id;
                _questionRepository.Create(questionEntity);
            }

            _questionRepository.Save();

            return quizEntity.Id;
        }

        public void Update(QuizEditRequest quiz, List<QuestionEditRequest>? questions, int callerId, bool isAdmin)
        {
            var quizEntity = _quizRepository.GetAll().Include(q => q.Questions).FirstOrDefault(q => q.Id == quiz.Id);
            EnsureCanChange(quizEntity, callerId, isAdmin);

            quizEntity.Title = quiz.Title;
            quizEntity.IsPrivate = quiz.IsPrivate;
            quizEntity.EntryCode = quiz.EntryCode;

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
                    // An id from another quiz would otherwise let a caller rewrite questions they do not own.
                    var questionEntity = quizEntity.Questions.FirstOrDefault(q => q.Id == questionRequest.Id);
                    if (questionEntity == null)
                    {
                        throw new ForbiddenException("Question does not belong to this quiz");
                    }

                    _mapper.Map(questionRequest, questionEntity);
                }
            }

            // The repositories share one context, so the quiz and its questions save together or not at all.
            _quizRepository.Save();
        }

        public void Delete(int id, int callerId, bool isAdmin)
        {
            var quizEntity = _quizRepository.ReadAll().FirstOrDefault(q => q.Id == id);
            EnsureCanChange(quizEntity, callerId, isAdmin);

            _quizRepository.Delete(id);
            _quizRepository.Save();
        }

        private static void EnsureCanChange([NotNull] Quiz? quiz, int callerId, bool isAdmin)
        {
            if (quiz == null)
            {
                throw new NotFoundException("Quiz not found");
            }

            if (!isAdmin && quiz.CreatorId != callerId)
            {
                throw new ForbiddenException("Only the quiz's creator can change it");
            }
        }

    }
}
