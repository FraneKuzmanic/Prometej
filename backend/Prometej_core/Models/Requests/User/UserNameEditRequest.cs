using System.ComponentModel.DataAnnotations;

namespace Prometej_core.Models.Requests.User
{
    public class UserNameEditRequest
    {
        [Required, StringLength(100)]
        public required string FirstName { get; set; }
        [Required, StringLength(100)]
        public required string LastName { get; set; }
    }
}
