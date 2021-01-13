using System.Linq;
using System.Threading.Tasks;
using API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers {
    public class AdminController : BaseApiController {
        private readonly UserManager<AppUser> _userManager;

        public AdminController(UserManager<AppUser> userManager) {
            _userManager = userManager;
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpGet("users-with-roles")]
        public async Task<ActionResult> GetUsersWithRoles() {
            var users = await _userManager.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .OrderBy(u => u.UserName)
                .Select(u => new {
                    u.Id, 
                    Username = u.UserName,
                    Roles = u.UserRoles.Select(r => r.Role.Name).ToList()
                })
                .ToListAsync();

            return Ok(users);
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpPost("edit-roles/{username}")]
        public async Task<ActionResult> EditRoles(string username, [FromQuery] string roles) {
            // Split the query string of roles
            var selectedRoles = roles.Split(",").ToArray();

            // Get the user from UserManager
            var user = await _userManager.FindByNameAsync(username);
            if (user == null) return NotFound("User with username " + username + " does not exist.");

            // Get the user's roles
            var userRoles = await _userManager.GetRolesAsync(user);

            // Add the user to the roles, except those they are already in
            var result = await _userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));

            if (!result.Succeeded) return BadRequest("Failed to add to roles");

            // Remove the user from the roles they were in before
            result = await _userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));

            if (!result.Succeeded) return BadRequest("Failed to remove from roles");

            return Ok(await _userManager.GetRolesAsync(user));
        }

        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpGet("photos-to-moderate")]
        public ActionResult GetPhotosForModeration() {
            return Ok("Only admins and moderators can see this");
        }
    }
}