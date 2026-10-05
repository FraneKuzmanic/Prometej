using System.Text.Json;
using System.Text.Json.Serialization;

namespace Prometej_core.Models.efModels
{
    // What a matching or an ordering question asks, stored as one JSON document. The numbers
    // a play is submitted with count these lists from 1, in the order they are stored in.
    public class QuestionContent
    {
        public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        // Matching: what goes with what.
        public List<MatchPair>? Pairs { get; set; }
        // Matching: right-hand options that go with nothing, numbered after the pairs' own.
        public List<string>? Extras { get; set; }
        // Ordering: the items in their right order.
        public List<string>? Items { get; set; }
    }

    public class MatchPair
    {
        public required string Left { get; set; }
        public required string Right { get; set; }
    }
}
