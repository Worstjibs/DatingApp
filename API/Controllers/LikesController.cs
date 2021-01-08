using System.Collections.Generic;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers {

    [Authorize]
    public class LikesController : BaseApiController {
        private readonly IUserRepository _userRepository;
        private readonly ILikesRepository _likesRepository;

        public LikesController(IUserRepository userRepository, ILikesRepository likesRepository) {
            _likesRepository = likesRepository;
            _userRepository = userRepository;
        }

        [HttpPost("{username}")]
        public async Task<ActionResult> AddLike(string username) {
            // Get the source user id from ClaimsPrincipal
            var sourceUserId = User.GetUserId();

            // Get the Liked and Source Users from the User and Likes Repositories
            var likedUser = await _userRepository.GetUserByUsernameAsync(username);
            var sourceUser = await _likesRepository.GetUserWithLikesAsync(sourceUserId);

            // Error checking on both Liked and Source Users
            if (likedUser == null) return NotFound("User with username " + username + " not found.");
            if (sourceUser.UserName == username) return BadRequest("You cannot like yourself.");

            // Error checking to see if the like already exists
            var userLike = await _likesRepository.GetUserLikeAsync(sourceUserId, likedUser.Id);
            if (userLike != null) return BadRequest("You already like this user.");

            // Create the UserLike record and add it to the source user's LikedUsers
            userLike = new UserLike {
                SourceUserId = sourceUserId,
                LikedUserId = likedUser.Id
            };
            sourceUser.LikedUsers.Add(userLike);

            if (await _userRepository.SaveAllAsync()) return Ok();

            return BadRequest("Error whilst saving UserRepository");
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LikeDto>>> GetUserLikes([FromQuery]LikesParams likesParams) {
            // Error checking on predicate before querying the LikesRepository
            if (likesParams.Predicate != "liked" && likesParams.Predicate != "likedBy") return BadRequest("Use either liked or likedBy as the predicate");

            // Set the UserId of LikesParams
            likesParams.UserId = User.GetUserId();

            var users = await _likesRepository.GetUserLikesAsync(likesParams);

            Response.AddPaginationHeader(likesParams.PageNumber, likesParams.PageSize, users.TotalCount, users.TotalPages);

            return Ok(users);
        }
    }
}