namespace Prometej_core.Models.ViewModels
{
    public class TopicViewModel
    {
        public int Id { get; set; }
        public int PeriodId { get; set; }
        public required string Title { get; set; }
        public required string Body { get; set; }
        // Null when the author's account was deleted.
        public string? AuthorName { get; set; }
        public string? AuthorRole { get; set; }
        public DateTime CreatedAt { get; set; }
        // Whether the caller may delete it; the client only shows or hides the button.
        public bool CanDelete { get; set; }
        public required List<ReplyViewModel> Replies { get; set; }
    }

    public class ReplyViewModel
    {
        public int Id { get; set; }
        public required string Body { get; set; }
        public string? AuthorName { get; set; }
        public string? AuthorRole { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool CanDelete { get; set; }
    }
}
