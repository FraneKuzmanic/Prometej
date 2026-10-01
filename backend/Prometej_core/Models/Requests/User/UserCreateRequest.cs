using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prometej_core.Models.Requests.User
{
    public class UserCreateRequest
    {
        [Required, StringLength(100)]
        public required string FirstName { get; set; }
        [Required, StringLength(100)]
        public required string LastName { get; set; }
        [Required, EmailAddress]
        public required string Email { get; set; }
        // 72 bytes is bcrypt's input limit; anything longer would be silently truncated.
        [Required, StringLength(72, MinimumLength = 8)]
        public required string Password { get; set; }
    }
}
