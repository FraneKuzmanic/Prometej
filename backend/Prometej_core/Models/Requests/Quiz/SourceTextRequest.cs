using System.ComponentModel.DataAnnotations;

namespace Prometej_core.Models.Requests.Quiz
{
    public class SourceTextRequest
    {
        // 0 for a source text added in this request.
        public int Id { get; set; }
        [Required, StringLength(200)]
        public required string Caption { get; set; }
        [Required, StringLength(8000)]
        public required string Body { get; set; }
    }
}
