using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prometej_core.Models.efModels;
using Prometej_core.Models.Requests.User;
using Prometej_core.Models.ViewModels;

namespace Prometej_core.Services.Contracts
{
    public interface IUserService
    {
        int Register(UserCreateRequest user);
        UserViewModel Login(UserLoginRequest user);
        UserViewModel GetCurrentUser(int id);
    }
}
