using System.Linq.Expressions;
using Prometej_core.Models.efModels;
using Prometej_core.Models.Requests.Sitting;
using Prometej_core.Models.ViewModels;

namespace Prometej_core.Services.Contracts
{
    public interface ISittingService
    {
        SittingInfoViewModel Info(int quizId, int? code, int callerId);
        (SittingViewModel Sitting, bool Created) Start(int quizId, int? code, int callerId);
        void SaveAnswer(int sittingId, SittingAnswerRequest request, int callerId);
        SittingResultViewModel Finish(int sittingId, int callerId);
        void Discard(int sittingId, int callerId);
        void Reset(int sittingId, int callerId, bool isAdmin);
        // Ends the running sittings among these whose time has run out. False when another
        // request was ending one at the same moment: the caller must then write nothing more.
        bool EndExpired(Expression<Func<Sitting, bool>> which);
    }
}
