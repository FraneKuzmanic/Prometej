using System;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Prometej_core.Auth;
using Prometej_core.DataAccessLayer;
using Prometej_core.Exceptions;
using Prometej_core.Models.efModels;
using Prometej_core.Models.Requests.User;
using Prometej_core.Models.ViewModels;
using Prometej_core.Services.Contracts;

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

        public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

        public UserViewModel GetCurrentUser(int id)
        {
            var userEntity = _userRepository.ReadAll().FirstOrDefault(u => u.Id == id);
            if (userEntity == null)
            {
                throw new NotFoundException("User not found");
            }

            UserViewModel userViewModel = _mapper.Map<UserViewModel>(userEntity);

            return userViewModel;
        }

        public int Register(UserCreateRequest user)
        {
            var email = NormalizeEmail(user.Email);
            if (_userRepository.ReadAll().Any(u => u.Email == email))
            {
                throw new ConflictException("Email is already registered");
            }

            var userEntity = new User
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = email,
                PasswordHash = PasswordHasher.Hash(user.Password),
                Role = Roles.Student,
            };
            _userRepository.Create(userEntity);

            try
            {
                _userRepository.Save();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                // Two registrations raced past the check above; the unique index caught the second.
                throw new ConflictException("Email is already registered");
            }

            return userEntity.Id;
        }

        public UserViewModel Login(UserLoginRequest user)
        {
            var email = NormalizeEmail(user.Email);
            var userEntity = _userRepository.ReadAll().FirstOrDefault(u => u.Email == email);
            if (userEntity == null || !PasswordHasher.Verify(user.Password, userEntity.PasswordHash))
            {
                // One message for both cases, so the response does not reveal which emails exist.
                throw new InvalidCredentialsException("Invalid credentials");
            }

            UserViewModel userViewModel = _mapper.Map<UserViewModel>(userEntity);

            return userViewModel;
        }

        public void Delete(int id)
        {
            _userRepository.Delete(id);
            _userRepository.Save();
        }
    }
}
