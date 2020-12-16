using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers {

    [Authorize]
    public class UsersController : BaseApiController {

        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IPhotoService _photoService;

        public UsersController(IUserRepository userRepository, IMapper mapper, IPhotoService photoService) {
            _photoService = photoService;
            _userRepository = userRepository;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberDto>>> GetUsers() {
            // Get the list of members from the UserRepository
            var users = await _userRepository.GetMembersAsync();

            return Ok(users);
        }

        [HttpGet("{username}", Name="GetUser")]
        public async Task<ActionResult<MemberDto>> GetUser(string username) {
            // Get a member by their UserName 
            return await _userRepository.GetMemberAsync(username);
        }

        [HttpPut]
        public async Task<ActionResult> UpdateUser(MemberUpdateDto memberUpdateDto) {
            // Get the user from the UserRepository, using current logged in user from claims?
            // uses extension method from ClaimsPrincipleExtensions
            var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());

            // Map the MemberDto to the AppUser
            _mapper.Map(memberUpdateDto, user);

            // Update the user in the UserRepository
            _userRepository.Update(user);

            // Save the UserRespository, return NoContent if successful
            if (await _userRepository.SaveAllAsync()) return NoContent();

            // Return a BadRequest if save fails
            return BadRequest("Failed to update user");
        }

        [HttpPost("add-photo")]
        public async Task<ActionResult<PhotoDto>> AddPhoto(IFormFile file) {
            // Get the user from the UserRepository, using current logged in user from claims?
            // uses extension method from ClaimsPrincipleExtensions
            var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());

            // Add the photo to Cloudinary
            var result = await _photoService.AddPhotoAsync(file);

            // If the photo upload fails, return a BadRequest
            if (result.Error != null) return BadRequest(result.Error.Message);

            // Create new Photo entity using result from Cloudinary
            var photo = new Photo {
                Url = result.SecureUrl.AbsoluteUri,
                PublicId = result.PublicId
            };

            // Set this photo to main if the user has no photos
            if (user.Photos.Count == 0) photo.IsMain = true;
            
            // Add to the user's photos
            user.Photos.Add(photo);

            // Save the UserRepository
            if (await _userRepository.SaveAllAsync()) {
                // Return a Created status if successful
                return CreatedAtRoute("GetUser", new {username = user.UserName}, _mapper.Map<PhotoDto>(photo));
            }

            // Return a BadRequest if save fails
            return BadRequest("Problem adding photo");
        }

        [HttpPut("set-main-photo/{photoId}")]
        public async Task<ActionResult> SetMainPhoto(int photoId) {
            // Get the user from the UserRepository, using current logged in user from claims?
            // uses extension method from ClaimsPrincipleExtensions
            var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());

            // Get the photo using the Id passed into the method
            var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);

            // If the photo is already main, return a BadRequest
            if (photo.IsMain) return BadRequest("This is already your main photo");

            // Get the current main photo to set to not main
            var currentMain = user.Photos.FirstOrDefault(x => x.IsMain);

            // Set the current main photo to not main if it exists
            if (currentMain != null) currentMain.IsMain = false;

            // Set the new photo to main
            photo.IsMain = true;

            // Save the UserRepository
            if (await _userRepository.SaveAllAsync()) return NoContent();

            // Return BadRequest if save fails
            return BadRequest("Failed to set main photo");
        }

        [HttpDelete("delete-photo/{photoId}")]
        public async Task<ActionResult> DeletePhoto(int photoId) {
            // Get the user from the UserRepository, using current logged in user from claims?
            // uses extension method from ClaimsPrincipleExtensions
            var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());

            // Get the photo using the Id passed into the method
            var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);

            // If the Photo is not found, return NotFound
            if (photo == null) return NotFound();

            // If the photo is main, return a BadRequest
            if (photo.IsMain) return BadRequest("You cannot delete your main photo");

            if (photo.PublicId != null) {
                // Use PhotoService to delete the photo from cloudinary
                var result = await _photoService.DeletePhotoAsync(photo.PublicId);
                if (result.Error != null) return BadRequest(result.Error.Message);
            }

            // Remove the photo from the Database
            user.Photos.Remove(photo);

            // Save the UserRepository
            if (await _userRepository.SaveAllAsync()) return Ok();

            // Return BadRequest if save fails
            return BadRequest("Failed to delete photo");
        }
    }
}