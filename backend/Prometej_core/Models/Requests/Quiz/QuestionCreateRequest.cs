using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prometej_core.Models.efModels;

namespace Prometej_core.Models.Requests.Quiz
{
    public class QuestionCreateRequest : IValidatableObject
    {
        [Required, StringLength(500)]
        public required string QuestionTitle { get; set; }
        // One of QuestionTypes. A request without it is a choice question.
        [Required]
        public string Type { get; set; } = QuestionTypes.Choice;
        // The four answers and the correct one: a choice question has all five, another type none.
        [StringLength(500)]
        public string? FirstAnswer { get; set; }
        [StringLength(500)]
        public string? SecondAnswer { get; set; }
        [StringLength(500)]
        public string? ThirdAnswer { get; set; }
        [StringLength(500)]
        public string? FourthAnswer { get; set; }
        // Which of the four answers is the correct one.
        [Range(1, 4)]
        public int? CorrectOption { get; set; }
        // The pairs of a matching question or the items of an ordering one.
        public QuestionContentRequest? Content { get; set; }
        [StringLength(1000)]
        public string? HintText { get; set; }
        [StringLength(1000)]
        public string? ExploreMore { get; set; }
        // The 1-based number of the question's source text in the request's list, if it has one.
        [Range(1, 100)]
        public int? SourceTextNo { get; set; }

        // What each type must and must not carry. That texts differ is checked by the service,
        // on the trimmed texts.
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            string?[] options = [FirstAnswer, SecondAnswer, ThirdAnswer, FourthAnswer];
            var hasOptionFields = options.Any(option => option != null) || CorrectOption != null;

            switch (Type)
            {
                case QuestionTypes.Choice:
                    if (options.Any(string.IsNullOrWhiteSpace) || CorrectOption == null)
                    {
                        yield return new ValidationResult("A choice question needs four answers and a correct option.", [nameof(Type)]);
                    }
                    if (Content != null)
                    {
                        yield return new ValidationResult("A choice question has no content.", [nameof(Content)]);
                    }
                    break;

                case QuestionTypes.Matching:
                    if (hasOptionFields || SourceTextNo != null)
                    {
                        yield return new ValidationResult("A matching question has no answer options and no source text.", [nameof(Type)]);
                    }
                    if (Content?.Pairs is not { Count: >= 3 and <= 5 } || Content.Extras is { Count: > 2 } || Content.Items != null)
                    {
                        yield return new ValidationResult("A matching question needs three to five pairs and at most two extra options.", [nameof(Content)]);
                    }
                    else if (!AreTexts(Content.Pairs.SelectMany(pair => new[] { pair?.Left, pair?.Right }).Concat(Content.Extras ?? [])))
                    {
                        yield return new ValidationResult("Every text of a matching question has 1 to 200 characters.", [nameof(Content)]);
                    }
                    break;

                case QuestionTypes.Ordering:
                    if (hasOptionFields || SourceTextNo != null)
                    {
                        yield return new ValidationResult("An ordering question has no answer options and no source text.", [nameof(Type)]);
                    }
                    if (Content?.Items is not { Count: >= 3 and <= 6 } || Content.Pairs != null || Content.Extras != null)
                    {
                        yield return new ValidationResult("An ordering question needs three to six items.", [nameof(Content)]);
                    }
                    else if (!AreTexts(Content.Items))
                    {
                        yield return new ValidationResult("Every item of an ordering question has 1 to 200 characters.", [nameof(Content)]);
                    }
                    break;

                default:
                    yield return new ValidationResult("The question type is not known.", [nameof(Type)]);
                    break;
            }
        }

        private static bool AreTexts(IEnumerable<string?> texts) =>
            texts.All(text => !string.IsNullOrWhiteSpace(text) && text.Length <= QuestionContentRequest.MaxTextLength);
    }

    public class QuestionContentRequest
    {
        public const int MaxTextLength = 200;

        [NoNullItems]
        public List<MatchPairRequest>? Pairs { get; set; }
        [NoNullItems]
        public List<string>? Extras { get; set; }
        [NoNullItems]
        public List<string>? Items { get; set; }
    }

    public class MatchPairRequest
    {
        [Required, StringLength(QuestionContentRequest.MaxTextLength)]
        public required string Left { get; set; }
        [Required, StringLength(QuestionContentRequest.MaxTextLength)]
        public required string Right { get; set; }
    }
}
