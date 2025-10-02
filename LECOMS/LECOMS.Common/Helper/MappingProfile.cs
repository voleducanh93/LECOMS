using AutoMapper;
using LECOMS.Data.DTOs.Auth;
using LECOMS.Data.DTOs.User;
using LECOMS.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Common.Helper
{
    public class MappingProfile : Profile
    {
        public MappingProfile() 
        {
            // User Mapping
            CreateMap<User, UserDTO>();

            CreateMap<UserRegisterDTO, User>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.Id, opt => opt.Ignore());
        
        }

    }
}
