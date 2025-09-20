using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Movement_Home_Task.Models.DTOs;
using Movement_Home_Task.Repositories;
using Movement_Home_Task.Services;

namespace Movement_Home_Task.Controllers
{
    /// <summary>
    /// User endpoints demonstrating multi-layered data storage (cache/file/db),
    /// JWT auth and role-based authorization.
    /// Base route: /data
    /// </summary>
    [ApiController]
    [Route("data")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IAuthService auth;
        private readonly IUserService userService;
        private readonly IUserRepository userRepository;

        /// <summary>
        /// Initializes a new UserController with domain services and repository.
        /// </summary>
        /// <param name="_auth">Authentication service that issues JWTs.</param>
        /// <param name="_user">User service orchestrating cache/file/db operations.</param>
        /// <param name="_rep">User repository for direct persistence queries.</param>
        public UserController(IAuthService _auth, IUserService _user, IUserRepository _rep)
        {
            auth = _auth;
            userService = _user;
            userRepository = _rep;
        }

        /// <summary>
        /// Gets a user by id.
        /// </summary>
        /// <param name="id">The user's identifier.</param>
        /// <returns>
        /// 200 OK with the user when found; 400 Bad Request if id is missing;
        /// 404 Not Found if no user exists with the given id.
        /// </returns>
        /// <remarks>Route: GET /data/{id}. Allows anonymous access.</remarks>
        [HttpGet("{id}")]
        [AllowAnonymous]
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

        /// <summary>
        /// Registers a new user.
        /// </summary>
        /// <param name="request">The registration payload (role, description, etc.).</param>
        /// <returns>
        /// 200 OK with the created user; 400 Bad Request if the request is missing or
        /// the registration process fails.
        /// </returns>
        /// <remarks>Route: POST /data. Requires the <c>Admin</c> role.</remarks>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        //[AllowAnonymous]
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

        /// <summary>
        /// Edits an existing user by id.
        /// </summary>
        /// <param name="request">The edit payload (fields to update).</param>
        /// <param name="id">The user's identifier to update.</param>
        /// <returns>
        /// 200 OK with the updated user; 400 Bad Request if input is invalid or the edit fails.
        /// </returns>
        /// <remarks>Route: PUT /data/{id}. Requires the <c>Admin</c> role.</remarks>
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

        /// <summary>
        /// Logs in by user id and returns a JWT token plus basic profile info.
        /// </summary>
        /// <param name="id">The user's identifier.</param>
        /// <returns>
        /// 200 OK with 'LoginByIdResponse' and jwtToken;
        /// 404 Not Found if the user does not exist or id is missing.
        /// </returns>
        /// <remarks>Route: GET /data?id=... . Allows anonymous access.</remarks>
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
