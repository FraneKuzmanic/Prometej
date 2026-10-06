using Prometej_core.Models.efModels;

namespace Prometej_core.Services.Implementations
{
    // What a play is worth, whichever way it was played: a practice submit and a sitting
    // both get their answer rows and their score here.
    internal static class QuizScoring
    {
        // The answer rows of one play and its score. `given` answers for a question with the
        // stored number chosen for each of its points (one for a choice question, one per pair,
        // one per place), null where nothing was given. A row is a point, answered or not.
        public static (List<Answer> Answers, int Score) Score(
            IEnumerable<Question> questionsInOrder, Func<Question, IReadOnlyList<int?>> given)
        {
            var answers = new List<Answer>();
            var score = 0;
            foreach (var question in questionsInOrder)
            {
                var numbers = given(question);
                var rows = question.Type switch
                {
                    QuestionTypes.Matching => MatchingRows(question, numbers),
                    QuestionTypes.Ordering => OrderingRows(question, numbers),
                    _ => ChoiceRows(question, numbers),
                };

                // A row is a point to win. The title, the explanation and both answers are kept
                // as text, so the play still reads right after the question is edited.
                foreach (var (row, isCorrect) in rows)
                {
                    row.QuestionId = question.Id;
                    row.QuestionTitle = question.QuestionTitle;
                    row.ExploreMore = question.ExploreMore;
                    row.SourceTextId = question.SourceTextId;
                    row.Position = answers.Count;
                    answers.Add(row);
                    if (isCorrect)
                    {
                        score++;
                    }
                }
            }

            return (answers, score);
        }

        // How many points a question gives, and so how many numbers answer it.
        public static int PointsOf(Question question) => question.Type switch
        {
            QuestionTypes.Matching => question.Content!.Pairs!.Count,
            QuestionTypes.Ordering => question.Content!.Items!.Count,
            _ => 1,
        };

        // The right-hand options of a matching question as a play numbers them: the pairs'
        // own first, then the extras.
        public static List<string> RightOptionsOf(Question question) =>
            question.Content!.Pairs!.Select(pair => pair.Right).Concat(question.Content.Extras ?? []).ToList();

        // Right and wrong are decided here by comparing numbers. The texts stored next to each
        // other say the same afterwards, because the texts a number can stand for all differ.
        // A point nobody answered has no chosen text, which no correct answer equals.
        private static List<(Answer Row, bool IsCorrect)> ChoiceRows(Question question, IReadOnlyList<int?> numbers)
        {
            string[] options = [question.FirstAnswer!, question.SecondAnswer!, question.ThirdAnswer!, question.FourthAnswer!];
            var row = new Answer
            {
                AnswerText = TextOf(options, numbers[0]),
                CorrectAnswer = options[question.CorrectOption!.Value - 1],
            };

            return [(row, numbers[0] == question.CorrectOption)];
        }

        // A row for each pair: its left item, the right-hand option chosen for it and the one
        // that belongs to it.
        private static List<(Answer Row, bool IsCorrect)> MatchingRows(Question question, IReadOnlyList<int?> numbers)
        {
            var rightOptions = RightOptionsOf(question);

            return question.Content!.Pairs!.Select((pair, i) => (
                new Answer { Item = pair.Left, AnswerText = TextOf(rightOptions, numbers[i]), CorrectAnswer = pair.Right },
                numbers[i] == i + 1)).ToList();
        }

        // A row for each place: the item put there and the item that belongs there.
        private static List<(Answer Row, bool IsCorrect)> OrderingRows(Question question, IReadOnlyList<int?> numbers)
        {
            var items = question.Content!.Items!;

            return items.Select((item, i) => (
                new Answer { Place = i + 1, AnswerText = TextOf(items, numbers[i]), CorrectAnswer = item },
                numbers[i] == i + 1)).ToList();
        }

        private static string TextOf(IReadOnlyList<string> texts, int? number) =>
            number == null ? "" : texts[number.Value - 1];
    }
}
