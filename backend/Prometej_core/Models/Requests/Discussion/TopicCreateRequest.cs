using System.ComponentModel.DataAnnotations;

namespace Prometej_core.Models.Requests.Discussion
{
    public class TopicCreateRequest
    {
        [Required, StringLength(150)]
        public required string Title { get; set; }
        [Required, StringLength(2000)]
        public required string Body { get; set; }
    }
}
