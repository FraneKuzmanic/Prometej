namespace Prometej_core.Models.efModels
{
    // An answer under a topic. Replies are a flat list: a reply has none of its own.
    public class Reply
    {
        public int Id { get; set; }
        public int TopicId { get; set; }
        public Topic Topic { get; set; }
        // Null once the author's account is deleted: the post stays.
        public int? AuthorId { get; set; }
        public User? Author { get; set; }
        // Plain text; its line breaks are kept.
        public required string Body { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
