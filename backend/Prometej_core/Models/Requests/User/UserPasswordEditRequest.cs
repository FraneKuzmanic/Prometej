using System.ComponentModel.DataAnnotations;

namespace Prometej_core.Models.Requests.User
{
    public class UserPasswordEditRequest
    {
        [Required]
        public required string CurrentPassword { get; set; }
        // 72 bytes is bcrypt's input limit; anything longer would be silently truncated.
        [Required, StringLength(72, MinimumLength = 8)]
        public required string NewPassword { get; set; }
    }
}
