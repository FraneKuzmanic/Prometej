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
        List<CreatorQuizViewModel> getAllUserQuizzes(int id);
        QuizViewModel GetQuiz(int id, int? code, int? callerId, bool isAdmin);
        QuizViewModel GetQuizByCode(int quizCode);
        int Create(QuizCreateRequest quiz, List<QuestionCreateRequest> questions, int creatorId);
        void Update(QuizEditRequest quiz, List<QuestionEditRequest>? questions, int callerId, bool isAdmin);
        void Delete(int id, int callerId, bool isAdmin);
        QuizGameViewModel SubmitQuiz(QuizSubmitRequest request, int userId);
        List<QuizGameViewModel> GetQuizAnalytics(int quizId, int callerId, bool isAdmin);
    }
}
