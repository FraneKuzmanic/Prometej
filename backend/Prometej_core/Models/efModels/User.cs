using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prometej_core.Models.efModels
{
    public class User
    {
        public int Id { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public  required string Email { get; set; }
        public required string PasswordHash { get; set; }
        public required string Role { get; set; }
        // Written into each session token and compared on every request. A password change
        // replaces it, which ends every session opened before the change.
        public Guid SessionStamp { get; set; } = Guid.NewGuid();
    }
}
