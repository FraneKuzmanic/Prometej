using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Prometej_core.DataAccessLayer;
using Prometej_core.Models.Base;
using Prometej_core.Models.efModels;
using Prometej_core.Models.Requests.Quiz;
using Prometej_core.Models.ViewModels;
using Prometej_core.Services.Contracts;
using System;
using System.Collections.Generic;
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
            var quizes = _quizRepository.ReadAll().Include(q => q.Creator).Where(q => q.Title.Contains(search) || search.Contains(q.Creator.FirstName) || search.Contains(q.Creator.LastName) || q.Creator.FirstName.Contains(search) || q.Creator.LastName.Contains(search)).ToList();
            List<QuizBaseModel> quizBaseModels = _mapper.Map<List<QuizBaseModel>>(quizes);

            return quizBaseModels;
        }

        public List<QuizBaseModel> getAllUserQuizzes(int id)
        {
            var quizes = _quizRepository.ReadAll().Include(q => q.Creator).Where(q => q.CreatorId == id).ToList();
            List<QuizBaseModel> quizBaseModels = _mapper.Map<List<QuizBaseModel>>(quizes);

            return quizBaseModels;
        }

        public QuizViewModel GetQuiz(int id)
        {
            var quiz = _quizRepository.ReadAll().Include(q => q.Creator).Include(q => q.Questions).FirstOrDefault(q => q.Id == id);
            var quizViewModel = _mapper.Map<QuizViewModel>(quiz);

            return quizViewModel;
        }

        public QuizViewModel GetQuizByCode(int quizCode)
        {
            var quiz = _quizRepository.ReadAll().Include(q => q.Creator).Include(q => q.Questions).FirstOrDefault(q => q.EntryCode == quizCode);
            var quizViewModel = _mapper.Map<QuizViewModel>(quiz);

            return quizViewModel;
        }

        public int Create(QuizCreateRequest quiz, List<QuestionCreateRequest> questions)
        {
            var quizEntity = _mapper.Map<Quiz>(quiz);
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

        public void Update(QuizEditRequest quiz, List<QuestionEditRequest>? questions)
        {
            var quizEntity = _mapper.Map<Quiz>(quiz);
            _quizRepository.Update(quizEntity);
            _quizRepository.Save();

            if (questions == null) return;
            foreach (var questionRequest in questions)
            {
                var questionEntity = _mapper.Map<Question>(questionRequest);
                questionEntity.QuizId = quizEntity.Id;

                if (questionRequest.Id == 0)
                {
                    _questionRepository.Create(questionEntity);
                }
                else
                {
                    _questionRepository.Update(questionEntity);
                }
            }

            _questionRepository.Save();
        }

        public void Delete(int id)
        {
            try
            {
                _quizRepository.Delete(id);
                _quizRepository.Save();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

    }
}
