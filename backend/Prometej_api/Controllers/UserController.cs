using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prometej_api.Auth;
using Prometej_core.Exceptions;
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
            var (token, expires) = _tokenService.Create(user);
            AuthCookie.Append(Response, token, expires);

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

        [Authorize]
        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            try
            {
                return Ok(_userService.GetCurrentUser(User.GetUserId()));
            }
            catch (NotFoundException)
            {
                // A valid token for an account that no longer exists is not a session.
                AuthCookie.Delete(Response);
                return Unauthorized();
            }
        }

        [Authorize]
        [HttpDelete("me")]
        public IActionResult DeleteCurrentUser()
        {
            _userService.Delete(User.GetUserId());
            AuthCookie.Delete(Response);

            return NoContent();
        }
    }
}
