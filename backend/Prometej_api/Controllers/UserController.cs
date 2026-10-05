using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prometej_api.Auth;
using Prometej_core.Auth;
using Prometej_core.Models.Requests.User;
using Prometej_core.Services.Contracts;

namespace Prometej_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly TokenService _tokenService;

        public UserController(IUserService userService, TokenService tokenService)
        {
            _userService = userService;
            _tokenService = tokenService;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public IActionResult RegisterUser(UserCreateRequest model)
        {
            return StatusCode(201, _userService.Register(model));
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public IActionResult LoginUser(UserLoginRequest model)
        {
            var user = _userService.Login(model);
            StartSession(user.Id);

            return Ok(user);
        }

        // Anonymous on purpose: an expired session must still be able to clear its cookie.
        [AllowAnonymous]
        [HttpPost("logout")]
        public IActionResult LogoutUser()
        {
            AuthCookie.Delete(Response);

            return NoContent();
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpGet]
        public IActionResult GetUsers()
        {
            return Ok(_userService.GetUsers());
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            return Ok(_userService.GetCurrentUser(User.GetUserId()));
        }

        [Authorize]
        [HttpPut("me")]
        public IActionResult UpdateName(UserNameEditRequest model)
        {
            _userService.UpdateName(User.GetUserId(), model);

            return NoContent();
        }

        [Authorize]
        [HttpPut("me/password")]
        public IActionResult ChangePassword(UserPasswordEditRequest model)
        {
            _userService.ChangePassword(User.GetUserId(), model);
            // The old cookie carries the stamp that was just replaced.
            StartSession(User.GetUserId());

            return NoContent();
        }

        [Authorize]
        [HttpDelete("me")]
        public IActionResult DeleteCurrentUser()
        {
            _userService.Delete(User.GetUserId());
            AuthCookie.Delete(Response);

            return NoContent();
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpPut("{id}/role")]
        public IActionResult SetRole(int id, UserRoleEditRequest model)
        {
            _userService.SetRole(id, model.Role, User.GetUserId());

            return NoContent();
        }

        private void StartSession(int userId)
        {
            var (token, expires) = _tokenService.Create(_userService.FindSession(userId)!);
            AuthCookie.Append(Response, token, expires);
        }
    }
}
