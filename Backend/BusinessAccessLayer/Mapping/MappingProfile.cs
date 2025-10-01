using AutoMapper;
using BusinessAccessLayer.DTOs;
using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Mapping
{
    public class MappingProfile : Profile
    {

        public MappingProfile()
        {

            CreateMap<MenuItem, MenuDTO>();
            CreateMap<MenuCategory, MenuCategoryDTO>();

            CreateMap<Combo, ComboDTO>();
            CreateMap<ComboItem, ComboItemDTO>();
        }
    }
}
