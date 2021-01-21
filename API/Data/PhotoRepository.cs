using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data {
    public class PhotoRepository : IPhotoRepository {
        private readonly DataContext _context;
        private readonly IMapper _mapper;

        public PhotoRepository(DataContext context, IMapper mapper) {
            _mapper = mapper;
            _context = context;
        }

        public async Task<Photo> GetPhotoById(int id) {
            return await _context.Photos
                .Include(x => x.AppUser)
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(x => x.Id == id);
        }

        public async Task<IEnumerable<PhotoForApprovalDto>> GetUnapprovedPhotos() {
            var query = _context.Photos
                .Where(x => x.IsApproved == false)
                .ProjectTo<PhotoForApprovalDto>(_mapper.ConfigurationProvider)
                .AsQueryable();

            query = query.IgnoreQueryFilters();

            return await query.ToListAsync();
        }

        public void RemovePhoto(Photo photo) {
            _context.Photos.Remove(photo);
        }
    }
}