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
        private readonly IRepository<Quiz> _quizRepository;
        private readonly IRepository<QuizGame> _quizGameRepository;
        public UserService(IMapper mapper, IRepository<User> userRepository, IRepository<Quiz> quizRepository, IRepository<QuizGame> quizGameRepository) {
            _mapper = mapper;
            _userRepository = userRepository;
            _quizRepository = quizRepository;
            _quizGameRepository = quizGameRepository;
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

        public UserSession? FindSession(int id)
        {
            return _userRepository.ReadAll().Where(u => u.Id == id)
                .Select(u => new UserSession(u.Id, u.Email, u.Role, u.SessionStamp))
                .FirstOrDefault();
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
                FirstName = user.FirstName.Trim(),
                LastName = user.LastName.Trim(),
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

        public void UpdateName(int id, UserNameEditRequest name)
        {
            var user = _userRepository.Get(id);
            user.FirstName = name.FirstName.Trim();
            user.LastName = name.LastName.Trim();

            // A quiz game keeps a copy of its player's name for the creator's analytics.
            var userName = user.FirstName + " " + user.LastName;
            foreach (var quizGame in _quizGameRepository.GetAll().Where(g => g.UserId == id))
            {
                quizGame.UserName = userName;
            }

            _userRepository.Save();
        }

        public void ChangePassword(int id, UserPasswordEditRequest passwords)
        {
            var user = _userRepository.Get(id);
            if (!PasswordHasher.Verify(passwords.CurrentPassword, user.PasswordHash))
            {
                throw new BadRequestException("Current password is incorrect");
            }

            user.PasswordHash = PasswordHasher.Hash(passwords.NewPassword);
            // Every token issued before now carries the old stamp and stops working.
            user.SessionStamp = Guid.NewGuid();
            _userRepository.Save();
        }

        // Every account an admin can manage, in the order they were created.
        public List<UserAccountViewModel> GetUsers()
        {
            var users = _userRepository.ReadAll().Where(u => u.Email != DemoCreator.Email).OrderBy(u => u.Id).ToList();
            var quizCounts = _quizRepository.ReadAll()
                .GroupBy(q => q.CreatorId).Select(g => new { CreatorId = g.Key, Count = g.Count() })
                .ToDictionary(g => g.CreatorId, g => g.Count);

            var accounts = _mapper.Map<List<UserAccountViewModel>>(users);
            foreach (var account in accounts)
            {
                account.QuizCount = quizCounts.GetValueOrDefault(account.Id);
            }

            return accounts;
        }

        public void SetRole(int id, string role, int callerId)
        {
            if (!Roles.All.Contains(role))
            {
                throw new BadRequestException("Unknown role");
            }

            // An admin cannot take their own role away by a slip; another admin has to.
            if (id == callerId)
            {
                throw new ForbiddenException("An admin cannot change their own role");
            }

            var user = _userRepository.GetAll().FirstOrDefault(u => u.Id == id && u.Email != DemoCreator.Email);
            if (user == null)
            {
                throw new NotFoundException("User not found");
            }

            if (user.Role == role)
            {
                return;
            }

            // A student cannot edit or delete quizzes, so theirs would be left with no creator
            // who can. The quizzes go first, as when an account is deleted.
            if (role == Roles.Student && _quizRepository.ReadAll().Any(q => q.CreatorId == id))
            {
                throw new ConflictException("Delete the user's quizzes before making them a student");
            }

            user.Role = role;
            _userRepository.Save();
        }

        public void Delete(int id)
        {
            // Deleting the account would delete its quizzes, and with them every quiz game
            // students played on those quizzes. The quizzes go first, one by one.
            if (_quizRepository.ReadAll().Any(q => q.CreatorId == id))
            {
                throw new ConflictException("Delete your quizzes before deleting your account");
            }

            _userRepository.Delete(id);
            _userRepository.Save();
        }
    }
}
