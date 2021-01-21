using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data {
    public class UserRepository : IUserRepository {
        private readonly DataContext _context;
        private readonly IMapper _mapper;

        public UserRepository(DataContext context, IMapper mapper) {
            _mapper = mapper;
            _context = context;
        }

        public async Task<PagedList<MemberDto>> GetMembersAsync(UserParams userParams) {
            // Setup a query for Members from the DB
            IQueryable<AppUser> query = _context.Users.AsQueryable();

            // Filter out the current user
            query = query.Where(u => u.UserName != userParams.CurrentUsername);
            // Filter for members of the opposite Gender by default
            if (userParams.Gender != "both") query = query.Where(u => u.Gender == userParams.Gender);

            var minDob = DateTime.Today.AddYears(-userParams.MaxAge - 1);
            var maxDob = DateTime.Today.AddYears(-userParams.MinAge);

            // Query for users between the min and max date of birth
            query = query.Where(u => minDob <= u.DateOfBirth && u.DateOfBirth <= maxDob);

            // New switch statement to order the query
            query = userParams.OrderBy switch {
                "createdOn" => query.OrderByDescending(u => u.CreatedOn),
                _ => query.OrderByDescending(u => u.LastActive)
            };

            IQueryable<MemberDto> source = query.ProjectTo<MemberDto>(_mapper.ConfigurationProvider).AsNoTracking();

            return await PagedList<MemberDto>.CreateAsync(source, userParams.PageNumber, userParams.PageSize);
        }

        public async Task<AppUser> GetUserByIdAsync(int id) {
            return await _context.Users.FindAsync(id);
        }

        public async Task<AppUser> GetUserByUsernameAsync(string username, bool isCurrentUser = false) {
            var query = _context.Users
                .Include(x => x.Photos)
                .Where(x => x.UserName == username)
                .AsQueryable();

            if (isCurrentUser) query = query.IgnoreQueryFilters();

            return await query.FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<AppUser>> GetUsersAsync() {
            return await _context.Users
                .Include(p => p.Photos)
                .ToListAsync();
        }

        public void Update(AppUser user) {
            _context.Entry(user).State = EntityState.Modified;
        }

        public async Task<MemberDto> GetMemberAsync(string username, bool isCurrentUser) {
            var query = _context.Users
                .Where(x => x.UserName == username)
                .ProjectTo<MemberDto>(_mapper.ConfigurationProvider)
                .AsQueryable();

            if (isCurrentUser) query = query.IgnoreQueryFilters();

            return await query.SingleOrDefaultAsync();
        }

        public async Task<string> GetUserGender(string username) {
            return await _context.Users
                .Where(x => x.UserName == username)
                .Select(x => x.Gender)
                .FirstOrDefaultAsync();
        }
    }
}