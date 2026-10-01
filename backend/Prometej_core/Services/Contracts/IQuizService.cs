using Prometej_core.Models.Base;
using Prometej_core.Models.Requests.Quiz;
using Prometej_core.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prometej_core.Services.Contracts
{
    public interface IQuizService
    {
        List<QuizBaseModel> getAllPublicQuizzes();

        List<QuizBaseModel> searchQuizzes(string search);
        List<QuizBaseModel> getAllUserQuizzes(int id);
        QuizViewModel GetQuiz(int id);
        QuizViewModel GetQuizByCode(int quizCode);
        int Create(QuizCreateRequest quiz, List<QuestionCreateRequest> questions);
        void Update(QuizEditRequest quiz, List<QuestionEditRequest>? questions);
        void Delete(int id);
    }
}
