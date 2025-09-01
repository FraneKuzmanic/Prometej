using AutoMapper;
using Prometej_core.Models.efModels;
using Prometej_core.Models.Requests.User;
using Prometej_core.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prometej_core.AutoMapper.Profiles
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<UserCreateRequest, User>();
            CreateMap<User, UserCreateRequest>();
            CreateMap<User, UserViewModel>();

        }
        
    }
}
