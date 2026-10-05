namespace Prometej_core.Models.ViewModels
{
    // One page of a period's discussion.
    public class TopicPageViewModel
    {
        public required List<TopicSummaryViewModel> Topics { get; set; }
        // Every topic of the period, not only this page's.
        public int Total { get; set; }
        public int PageSize { get; set; }
    }

    public class TopicSummaryViewModel
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        // Null when the author's account was deleted.
        public string? AuthorName { get; set; }
        public string? AuthorRole { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ReplyCount { get; set; }
        // The newest reply's time, or the topic's own.
        public DateTime LastActivityAt { get; set; }
    }
}
