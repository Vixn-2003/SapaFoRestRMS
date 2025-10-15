using AutoMapper;
using BusinessAccessLayer.DTOs;
using DomainAccessLayer.Models;

namespace BusinessAccessLayer.Mapping
{
    public class MarketingCampaignMappingProfile : Profile
    {
        public MarketingCampaignMappingProfile()
        {
            // MarketingCampaign -> MarketingCampaignDto
            CreateMap<MarketingCampaign, MarketingCampaignDto>();

            // MarketingCampaignCreateDto -> MarketingCampaign
            CreateMap<MarketingCampaignCreateDto, MarketingCampaign>()
                .ForMember(dest => dest.CampaignId, opt => opt.Ignore())
                .ForMember(dest => dest.ViewCount, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.RevenueGenerated, opt => opt.MapFrom(src => 0m))
                .ForMember(dest => dest.CreatedByNavigation, opt => opt.Ignore())
                .ForMember(dest => dest.Voucher, opt => opt.Ignore());

            // MarketingCampaignUpdateDto -> MarketingCampaign
            CreateMap<MarketingCampaignUpdateDto, MarketingCampaign>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}