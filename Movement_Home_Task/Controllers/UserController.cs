using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Movement_Home_Task.Models.DTOs;
using Movement_Home_Task.Repositories;
using Movement_Home_Task.Services;

namespace Movement_Home_Task.Controllers
{
    [ApiController]
    [Route("data")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IAuthService auth;
        private readonly IUserService userService;
        private readonly IUserRepository userRepository;

        public UserController(IAuthService _auth, IUserService _user, IUserRepository _rep)
        {
            auth = _auth;
            userService = _user;
            userRepository = _rep;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Data is missing- User's id is required");
            }

            var user = await userService.GetUser(id);

            if (user == null)
            {
                return NotFound(new { error = $"User with Id {id} not found" });
            }

            return Ok(user);
        }

        [HttpPost]
        //[Authorize(Roles = "Admin")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (request == null)
            {
                return BadRequest("Data is missing- Full Register request is required");
            }

            var user = await userService.CreateUser(request);

            if (user == null)
            {
                return BadRequest(new { error = "Registration process failed" });
            }

            return Ok(user);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditUser([FromBody] EditRequest request, string id)
        {
            if (request == null)
            {
                return BadRequest("Data is missing- Edit request is required");
            }

            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Data is missing- User id is required");
            }

            var user = await userService.EditUser(id, request);

            if (user == null)
            {
                return BadRequest(new { error = "Edit user process failed" });
            }
            
            return Ok(user);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromQuery] string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound(new { error = "User not found" });

            var (user, jwt) = await auth.TokenRetrie(id);
            if (user is null || jwt is null)
                return NotFound(new { error = "User not found" });

            var resp = new LoginByIdResponse(
                Id: user.Id!,
                Role: user.Role.ToString()!,
                CreatedAt: user.CreatedAt,
                JwtToken: jwt
            );

            return Ok(resp);
        }
    }
}
