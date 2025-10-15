using AutoMapper;
using LECOMS.Data.DTOs.Auth;
using LECOMS.Data.DTOs.Course;
using LECOMS.Data.DTOs.Seller;
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
            // ----------------- SHOP & SELLER -----------------
            CreateMap<Shop, ShopDTO>()
                .ForMember(d => d.CategoryId, o => o.MapFrom(s => s.CategoryId))
                .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category.Name))
                .ReverseMap();
            CreateMap<Shop, ShopDTO>()
                .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category.Name))
                .ReverseMap();

            CreateMap<SellerRegistrationRequestDTO, Shop>()
                .ForMember(d => d.Name, o => o.MapFrom(s => s.ShopName))
                .ForMember(d => d.Description, o => o.MapFrom(s => s.ShopDescription))
                .ForMember(d => d.PhoneNumber, o => o.MapFrom(s => s.ShopPhoneNumber))
                .ForMember(d => d.Address, o => o.MapFrom(s => s.ShopAddress))
                .ForMember(d => d.BusinessType, o => o.MapFrom(s => s.BusinessType))
                .ForMember(d => d.OwnershipDocumentUrl, o => o.MapFrom(s => s.OwnershipDocumentUrl))
                // ✅ cập nhật CategoryId thay cho Category
                .ForMember(d => d.CategoryId, o => o.MapFrom(s => s.CategoryId))
                // ❌ bỏ dòng cũ `.ForMember(d => d.Category, ...)`
                .ForMember(d => d.AcceptedTerms, o => o.MapFrom(s => s.AcceptedTerms))
                .ForMember(d => d.OwnerFullName, o => o.MapFrom(s => s.OwnerFullName))
                .ForMember(d => d.OwnerDateOfBirth, o => o.MapFrom(s => s.OwnerDateOfBirth))
                .ForMember(d => d.OwnerPersonalIdNumber, o => o.MapFrom(s => s.OwnerPersonalIdNumber))
                .ForMember(d => d.OwnerPersonalIdFrontUrl, o => o.MapFrom(s => s.OwnerPersonalIdFrontUrl))
                .ForMember(d => d.OwnerPersonalIdBackUrl, o => o.MapFrom(s => s.OwnerPersonalIdBackUrl))
                .ForMember(d => d.Status, o => o.Ignore())        // sẽ được set trong service
                .ForMember(d => d.CreatedAt, o => o.Ignore())     // tự sinh khi tạo
                .ForMember(d => d.ApprovedAt, o => o.Ignore());   // tự sinh khi duyệt

            CreateMap<ShopUpdateDTO, Shop>()
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            // ----------------- COURSE CATEGORY -----------------
            CreateMap<CourseCategory, CourseCategoryDTO>().ReverseMap();
            CreateMap<CourseCategoryCreateDTO, CourseCategory>()
                .ForMember(d => d.Id, o => o.Ignore());

            // ----------------- LESSON -----------------
            CreateMap<Lesson, LessonDto>();
            CreateMap<CreateLessonDto, Lesson>()
                .ForMember(d => d.Id, o => o.Ignore());
        }
    }
}