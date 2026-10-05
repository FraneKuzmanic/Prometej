namespace Prometej_core.Models.efModels
{
    // A question or a remark in a period's discussion, with the replies under it.
    public class Topic
    {
        public int Id { get; set; }
        public int PeriodId { get; set; }
        public Period Period { get; set; }
        // Null once the author's account is deleted: the post stays.
        public int? AuthorId { get; set; }
        public User? Author { get; set; }
        public required string Title { get; set; }
        // Plain text; its line breaks are kept.
        public required string Body { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<Reply> Replies { get; set; } = [];
    }
}
