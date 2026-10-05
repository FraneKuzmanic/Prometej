namespace Prometej_core.Models.efModels
{
    // A passage (a poem, an excerpt) that some of a quiz's questions are asked about.
    public class SourceText
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public Quiz Quiz { get; set; }
        // Who wrote it and what it is from.
        public required string Caption { get; set; }
        // Plain text; its line breaks are kept.
        public required string Body { get; set; }
        // Changed or removed after it was played: out of the editor and of new plays,
        // kept for the answers given beside it.
        public bool IsRetired { get; set; }
    }
}
