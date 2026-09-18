using AutoMapper;
using Slush.Domain.Entities;
using Slush.Application.DTOs.Admin;
using Slush.Application.DTOs.Alerts;
using Slush.Application.DTOs.Audit;
using Slush.Application.DTOs.Monitoring;

namespace Slush.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<User, UserListDto>()
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));

            CreateMap<AlertRule, AlertRuleDto>();
            CreateMap<AlertHistory, AlertHistoryDto>();

            CreateMap<CreateAlertRuleRequestDto, AlertRule>();

            CreateMap<AdminActionLog, AdminActionLogDto>();

            CreateMap<ActivitySnapshot, ActivityPointDto>();
        }
    }
}