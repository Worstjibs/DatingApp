using System;
using System.Linq;
using API.DTOs;
using API.Entities;
using API.Extensions;
using AutoMapper;

namespace API.Helpers {

    // A Class used to configure AutoMapper to map data entities to DTOs
    public class AutoMapperProfiles : Profile {
        public AutoMapperProfiles() {
            // Map the AppUser entity to a MemberDto
            CreateMap<AppUser, MemberDto>()
                // Get the main Url of the member
                .ForMember(dest => dest.PhotoUrl, opt => opt.MapFrom(src => 
                    src.Photos.FirstOrDefault(p => p.IsMain == true).Url))
                // Calculate the age of the member using the DateOfBirth of the user
                // Utilizes DateTimeExtensions to calculate the age
                .ForMember(dest => dest.Age, opt => opt.MapFrom(src => 
                    src.DateOfBirth.CalculateAge()
                ));

            // Map the Photo entity to a PhotoDto
            CreateMap<Photo, PhotoDto>();

            // Map MemberUpdateDtos to AppUsers
            CreateMap<MemberUpdateDto, AppUser>();

            CreateMap<RegisterDto, AppUser>();

            // Map from Message to MessageDto
            CreateMap<Message, MessageDto>()
                .ForMember(dest => dest.SenderPhotoUrl, opt => opt.MapFrom(src => 
                    src.Sender.Photos.FirstOrDefault(p => p.IsMain == true).Url))
                .ForMember(dest => dest.RecipientPhotoUrl, opt => opt.MapFrom(src => 
                    src.Recipient.Photos.FirstOrDefault(p => p.IsMain == true).Url));
        }
    }
}