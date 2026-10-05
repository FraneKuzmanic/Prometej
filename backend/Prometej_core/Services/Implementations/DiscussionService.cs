using Prometej_core.DataAccessLayer;
using Prometej_core.Exceptions;
using Prometej_core.Models.efModels;
using Prometej_core.Models.Requests.Discussion;
using Prometej_core.Models.ViewModels;
using Prometej_core.Services.Contracts;

namespace Prometej_core.Services.Implementations
{
    public class DiscussionService : IDiscussionService
    {
        public const int PageSize = 20;

        private readonly IRepository<Topic> _topicRepository;
        private readonly IRepository<Reply> _replyRepository;
        private readonly IRepository<Period> _periodRepository;

        public DiscussionService(IRepository<Topic> topicRepository, IRepository<Reply> replyRepository, IRepository<Period> periodRepository)
        {
            _topicRepository = topicRepository;
            _replyRepository = replyRepository;
            _periodRepository = periodRepository;
        }

        // The topics with the newest activity first: a topic's own time, or its newest reply's.
        public TopicPageViewModel GetTopics(int periodId, int page)
        {
            CheckPeriodExists(periodId);

            var topics = _topicRepository.ReadAll().Where(t => t.PeriodId == periodId);
            // The name and the role are the author's of today; nothing is copied onto a post.
            var pageOfTopics = topics
                .Select(t => new TopicSummaryViewModel
                {
                    Id = t.Id,
                    Title = t.Title,
                    AuthorName = t.Author == null ? null : t.Author.FirstName + " " + t.Author.LastName,
                    AuthorRole = t.Author == null ? null : t.Author.Role,
                    CreatedAt = t.CreatedAt,
                    ReplyCount = t.Replies.Count,
                    LastActivityAt = t.Replies.Max(r => (DateTime?)r.CreatedAt) ?? t.CreatedAt,
                })
                .OrderByDescending(t => t.LastActivityAt).ThenByDescending(t => t.Id)
                .Skip((page - 1) * PageSize).Take(PageSize)
                .ToList();

            return new TopicPageViewModel { Topics = pageOfTopics, Total = topics.Count(), PageSize = PageSize };
        }

        public TopicViewModel GetTopic(int id, int? callerId, bool isAdmin)
        {
            var topic = _topicRepository.ReadAll()
                .Where(t => t.Id == id)
                .Select(t => new
                {
                    t.Id,
                    t.PeriodId,
                    t.Title,
                    t.Body,
                    t.AuthorId,
                    AuthorName = t.Author == null ? null : t.Author.FirstName + " " + t.Author.LastName,
                    AuthorRole = t.Author == null ? null : t.Author.Role,
                    t.CreatedAt,
                    Replies = t.Replies.OrderBy(r => r.CreatedAt).ThenBy(r => r.Id)
                        .Select(r => new
                        {
                            r.Id,
                            r.Body,
                            r.AuthorId,
                            AuthorName = r.Author == null ? null : r.Author.FirstName + " " + r.Author.LastName,
                            AuthorRole = r.Author == null ? null : r.Author.Role,
                            r.CreatedAt,
                        })
                        .ToList(),
                })
                .FirstOrDefault() ?? throw new NotFoundException("Topic not found");

            return new TopicViewModel
            {
                Id = topic.Id,
                PeriodId = topic.PeriodId,
                Title = topic.Title,
                Body = topic.Body,
                AuthorName = topic.AuthorName,
                AuthorRole = topic.AuthorRole,
                CreatedAt = topic.CreatedAt,
                CanDelete = MayDeleteTopic(topic.AuthorId, topic.Replies.Count > 0, callerId, isAdmin),
                Replies = topic.Replies
                    .Select(r => new ReplyViewModel
                    {
                        Id = r.Id,
                        Body = r.Body,
                        AuthorName = r.AuthorName,
                        AuthorRole = r.AuthorRole,
                        CreatedAt = r.CreatedAt,
                        CanDelete = MayDeleteReply(r.AuthorId, callerId, isAdmin),
                    })
                    .ToList(),
            };
        }

        public int CreateTopic(int periodId, TopicCreateRequest topic, int callerId)
        {
            CheckPeriodExists(periodId);

            var topicEntity = new Topic
            {
                PeriodId = periodId,
                AuthorId = callerId,
                Title = topic.Title.Trim(),
                Body = Clean(topic.Body),
                CreatedAt = DateTime.UtcNow,
            };
            _topicRepository.Create(topicEntity);
            _topicRepository.Save();

            return topicEntity.Id;
        }

        public int CreateReply(int topicId, ReplyCreateRequest reply, int callerId)
        {
            if (!_topicRepository.ReadAll().Any(t => t.Id == topicId))
            {
                throw new NotFoundException("Topic not found");
            }

            var replyEntity = new Reply
            {
                TopicId = topicId,
                AuthorId = callerId,
                Body = Clean(reply.Body),
                CreatedAt = DateTime.UtcNow,
            };
            _replyRepository.Create(replyEntity);
            _replyRepository.Save();

            return replyEntity.Id;
        }

        public void DeleteTopic(int id, int callerId, bool isAdmin)
        {
            var topic = _topicRepository.ReadAll()
                .Where(t => t.Id == id)
                .Select(t => new { t.AuthorId, HasReplies = t.Replies.Any() })
                .FirstOrDefault() ?? throw new NotFoundException("Topic not found");

            if (!MayDeleteTopic(topic.AuthorId, topic.HasReplies, callerId, isAdmin))
            {
                if (IsAuthor(topic.AuthorId, callerId))
                {
                    throw new ConflictException("A topic with replies can only be deleted by an admin");
                }

                throw new ForbiddenException("Only the author or an admin can delete a topic");
            }

            // Its replies go with it: the database cascades.
            _topicRepository.Delete(id);
            _topicRepository.Save();
        }

        public void DeleteReply(int id, int callerId, bool isAdmin)
        {
            var reply = _replyRepository.ReadAll()
                .Where(r => r.Id == id)
                .Select(r => new { r.AuthorId })
                .FirstOrDefault() ?? throw new NotFoundException("Reply not found");

            if (!MayDeleteReply(reply.AuthorId, callerId, isAdmin))
            {
                throw new ForbiddenException("Only the author or an admin can delete a reply");
            }

            _replyRepository.Delete(id);
            _replyRepository.Save();
        }

        private void CheckPeriodExists(int periodId)
        {
            if (!_periodRepository.ReadAll().Any(p => p.Id == periodId))
            {
                throw new NotFoundException("Period not found");
            }
        }

        // An author deletes their topic only while nobody has replied: deleting it would
        // delete what other people answered. An admin deletes any.
        private static bool MayDeleteTopic(int? authorId, bool hasReplies, int? callerId, bool isAdmin) =>
            isAdmin || (IsAuthor(authorId, callerId) && !hasReplies);

        private static bool MayDeleteReply(int? authorId, int? callerId, bool isAdmin) =>
            isAdmin || IsAuthor(authorId, callerId);

        // A post whose author's account was deleted is nobody's.
        private static bool IsAuthor(int? authorId, int? callerId) => authorId != null && authorId == callerId;

        // A body keeps its line breaks, stored as "\n" whatever the browser sent.
        private static string Clean(string text) => text.Trim().Replace("\r\n", "\n").Replace('\r', '\n');
    }
}
