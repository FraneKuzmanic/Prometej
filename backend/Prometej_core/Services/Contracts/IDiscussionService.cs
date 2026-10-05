using Prometej_core.Models.Requests.Discussion;
using Prometej_core.Models.ViewModels;

namespace Prometej_core.Services.Contracts
{
    public interface IDiscussionService
    {
        TopicPageViewModel GetTopics(int periodId, int page);
        TopicViewModel GetTopic(int id, int? callerId, bool isAdmin);
        int CreateTopic(int periodId, TopicCreateRequest topic, int callerId);
        int CreateReply(int topicId, ReplyCreateRequest reply, int callerId);
        void DeleteTopic(int id, int callerId, bool isAdmin);
        void DeleteReply(int id, int callerId, bool isAdmin);
    }
}
