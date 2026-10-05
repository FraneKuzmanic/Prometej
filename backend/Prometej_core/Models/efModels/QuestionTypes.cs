namespace Prometej_core.Models.efModels
{
    public static class QuestionTypes
    {
        // Four options, one of them correct.
        public const string Choice = "choice";
        // Pairs to connect: a point for each right pair.
        public const string Matching = "matching";
        // Items to put in order: a point for each item at its place.
        public const string Ordering = "ordering";
    }
}
