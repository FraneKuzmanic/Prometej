using System.ComponentModel.DataAnnotations;

namespace Prometej_core.Models.Requests.Discussion
{
    public class ReplyCreateRequest
    {
        [Required, StringLength(2000)]
        public required string Body { get; set; }
    }
}
