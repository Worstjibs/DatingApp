using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers {
    public class AccountController : BaseApiController {
        private readonly DataContext _context;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;

        public AccountController(DataContext context, ITokenService tokenService, IMapper mapper) {
            _mapper = mapper;
            _tokenService = tokenService;
            _context = context;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto) {
            // Check if the RegisterDto username is already taken
            if (await UserExists(registerDto.Username)) return BadRequest("Username is taken!");

            // Create new AppUser entity
            var user = _mapper.Map<AppUser>(registerDto);

            // Using HMAC object for Crypto
            using var hmac = new HMACSHA512();

            /// Add the Password details to the new User entity
            user.UserName = registerDto.Username.ToLower();
            user.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password));
            user.PasswordSalt = hmac.Key;

            // Add the new entity to the Users list in DataContext
            _context.Users.Add(user);
            // Save changes
            await _context.SaveChangesAsync();

            // Return a UserDto using the Username and Token of the created user
            return new UserDto
            {
                Username = user.UserName,
                // Create the token for the created user here
                Token = _tokenService.CreateToken(user),
                KnownAs = user.KnownAs
            };
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto) {
            // Get the User by username from DataContext
            var user = await _context.Users
                .Include(p => p.Photos)
                .SingleOrDefaultAsync(user => user.UserName == loginDto.Username.ToLower());

            if (user == null) return Unauthorized("Invalid Username or Password");

            using var hamc = new HMACSHA512(user.PasswordSalt);

            // Hash the password provide in the LoginDto
            var computedHash = hamc.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

            // Compare the computed hash with the user's hash
            if (computedHash.SequenceEqual(user.PasswordHash)) {
                var photos = user.Photos;
                string photoUrl = user.Photos.FirstOrDefault(x => x.IsMain)?.Url;

                UserDto userDto = new UserDto
                {
                    Username = user.UserName,
                    Token = _tokenService.CreateToken(user),
                    // Add the user's main photo to the UserDto
                    PhotoUrl = photoUrl,
                    KnownAs = user.KnownAs,
                    Gender = user.Gender
                };

                return userDto;
            } else {
                return Unauthorized("Invalid Username or Password");
            }
        }

        private async Task<bool> UserExists(string username) {
            return await _context.Users.AnyAsync(user => user.UserName == username.ToLower());
        }
    }
}