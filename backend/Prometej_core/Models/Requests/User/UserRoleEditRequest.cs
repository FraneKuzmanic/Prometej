using System.ComponentModel.DataAnnotations;

namespace Prometej_core.Models.Requests.User
{
    public class UserRoleEditRequest
    {
        [Required]
        public required string Role { get; set; }
    }
}
