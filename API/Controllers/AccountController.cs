using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers {
    public class AccountController : BaseApiController {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;

        public AccountController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            ITokenService tokenService, 
            IMapper mapper
        ) {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _mapper = mapper;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto) {
            // Check if the RegisterDto username is already taken
            if (await UserExists(registerDto.Username)) return BadRequest("Username is taken!");

            // Create new AppUser entity
            var user = _mapper.Map<AppUser>(registerDto);

            /// Add the Password details to the new User entity
            user.UserName = registerDto.Username.ToLower();

            // Create the user using UserManager
            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded) return BadRequest(result.Errors);

            // Add the new User to the Member role by default
            var roleResult = await _userManager.AddToRoleAsync(user, "Member");
            if (!roleResult.Succeeded) return BadRequest(result.Errors);

            // Return a UserDto using the Username and Token of the created user
            return new UserDto
            {
                Username = user.UserName,
                // Create the token for the created user here
                Token = await _tokenService.CreateTokenAsync(user),
                KnownAs = user.KnownAs,
                // Add Gender for default filtering
                Gender = user.Gender
            };
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto) {
            // Get the User by username from DataContext
            var user = await _userManager.Users
                .Include(p => p.Photos)
                .SingleOrDefaultAsync(user => user.UserName == loginDto.Username.ToLower());

            if (user == null) return Unauthorized("Invalid Username or Password");

            // Login with SignInManager
            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);

            if (!result.Succeeded) return Unauthorized();

            UserDto userDto = new UserDto
            {
                Username = user.UserName,
                Token = await _tokenService.CreateTokenAsync(user),
                // Add the user's main photo to the UserDto
                PhotoUrl = user.Photos.FirstOrDefault(x => x.IsMain)?.Url,
                // Add KnownAs to display in the top right
                KnownAs = user.KnownAs,
                // Add Gender for default filtering
                Gender = user.Gender
            };

            return userDto;
        }

        private async Task<bool> UserExists(string username) {
            return await _userManager.Users.AnyAsync(user => user.UserName == username.ToLower());
        }
    }
}