using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VTOS.Application.Features.Children.DTOs;
using VTOS.Domain.Entities;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace VTOS.Application.Features.Children.Mappings
{
    public class ChildProfileMappingProfile : Profile
    {
        public ChildProfileMappingProfile()
        {
            CreateMap<ChildProfile, ChildBodyMetricDto>()
            .ForMember(dest => dest.HeightCm,
                opt => opt.MapFrom(src => src.HeightCm))
            .ForMember(dest => dest.WeightKg,
                opt => opt.MapFrom(src => src.WeightKg));

            CreateMap<ChildProfile, GetChildProfileResponse>()
            // Id
            .ForMember(dest => dest.ChildId,
                opt => opt.MapFrom(src => src.Id))

            // Basic info
            .ForMember(dest => dest.FullName,
                opt => opt.MapFrom(src => src.FullName))
            .ForMember(dest => dest.Age,
                opt => opt.MapFrom(src => src.Age))
            .ForMember(dest => dest.Grade,
                opt => opt.MapFrom(src => src.Grade))

            // Enum → string
            .ForMember(dest => dest.Gender,
                opt => opt.MapFrom(src => src.Gender.ToString()))

            // School
            .ForMember(dest => dest.SchoolId,
                opt => opt.MapFrom(src => src.SchoolID))
            .ForMember(dest => dest.SchoolName,
                opt => opt.MapFrom(src => src.School.SchoolName))

            // Avatar
            .ForMember(dest => dest.AvatarUrl,
                opt => opt.MapFrom(src => src.Avatar))

            // Nested object
            .ForMember(dest => dest.BodyMetric,
                opt => opt.MapFrom(src => src))

            // Business logic
            .ForMember(dest => dest.IsStandardSize,
                opt => opt.MapFrom(src =>
                    IsPhysicallyPossible(src.HeightCm, src.WeightKg)
                ));
            }
        public static bool IsPhysicallyPossible(int heightCm, float weightKg)
        {
            return heightCm >= 50 && heightCm <= 200
                && weightKg >= 5 && weightKg <= 120;
        }
    }
}
