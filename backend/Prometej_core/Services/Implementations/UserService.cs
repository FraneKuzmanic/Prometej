using System;
using AutoMapper;
using Prometej_core.DataAccessLayer;
using Prometej_core.Models.efModels;
using Prometej_core.Models.Requests.User;
using Prometej_core.Models.ViewModels;
using Prometej_core.Services.Contracts;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace Prometej_core.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IMapper _mapper;
        private readonly IRepository<User> _userRepository;
        public UserService(IMapper mapper, IRepository<User> userRepository) {
            _mapper = mapper;
            _userRepository = userRepository;
        }

        public UserViewModel GetCurrentUser(int id)
        {
            var userEntity = _userRepository.ReadAll().FirstOrDefault(u => u.Id == id);
            if (userEntity == null)
            {
                throw new Exception("User not found");
            }

            UserViewModel userViewModel = _mapper.Map<UserViewModel>(userEntity);

            return userViewModel;
        }
        public int Register(UserCreateRequest user)
        {
            var userEntity = _mapper.Map<User>(user);
            _userRepository.Create(userEntity);
            _userRepository.Save();

            return userEntity.Id;
        }

        public UserViewModel Login(UserLoginRequest user)
        {
            var userEntity = _userRepository.ReadAll().FirstOrDefault(u => u.Email == user.Email && u.Password == user.Password);
            if (userEntity == null)
            {
                throw new Exception("Invalid credentials");
            }

            UserViewModel userViewModel = _mapper.Map<UserViewModel>(userEntity);

            return userViewModel;
        }
    }
}
