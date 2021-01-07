using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data {
    public class LikesRepository : ILikesRepository {
        private readonly DataContext _context;
        public LikesRepository(DataContext context) {
            _context = context;
        }

        public async Task<UserLike> GetUserLikeAsync(int sourceUserId, int likedUserId) {
            return await _context.Likes.FindAsync(sourceUserId, likedUserId);
        }

        public async Task<PagedList<LikeDto>> GetUserLikesAsync(LikesParams likesParams) {
            // Get the users and likes as Queryables
            var users = _context.Users.OrderBy(u => u.UserName).AsQueryable();
            var likes = _context.Likes.AsQueryable();

            string predicate = likesParams.Predicate;
            int userId = likesParams.UserId;

            // Select the users that the user has liked
            if (predicate == "liked") {
                likes = likes.Where(like => like.SourceUserId == userId);
                users = likes.Select(like => like.LikedUser);
            }
            // Select the users that have liked the user
            else if (predicate == "likedBy") {
                likes = likes.Where(like => like.LikedUserId == userId);
                users = likes.Select(like => like.SourceUser);
            }
            // Else, throw an exception
            else {
                throw new Exception("Use either liked or likedBy as the predicate");
            }

            // Project to a LikeDto and return a list
            var likedUsers = users.Select(user => new LikeDto {
                Id = user.Id,
                Username = user.UserName,
                Age = user.DateOfBirth.CalculateAge(),
                KnownAs = user.KnownAs,
                PhotoUrl = user.Photos.FirstOrDefault(p => p.IsMain == true).Url,
                City =  user.City
            });

            return await PagedList<LikeDto>.CreateAsync(likedUsers, likesParams.PageNumber, likesParams.PageSize);
        }

        public async Task<AppUser> GetUserWithLikesAsync(int userId) {
            return await _context.Users
                .Include(x => x.LikedUsers)
                .FirstOrDefaultAsync(x => x.Id == userId);
        }
    }
}