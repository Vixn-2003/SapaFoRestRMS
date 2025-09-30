using AutoMapper;
using BusinessAccessLayer.DTOs;
using DomainAccessLayer.Models;

namespace BusinessAccessLayer.Mapping
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<SystemLogo, SystemLogoDto>().ReverseMap();
        }
    }
}
