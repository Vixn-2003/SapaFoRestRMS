using AutoMapper;
using BusinessAccessLayer.DTOs.Inventory;
using BusinessAccessLayer.DTOs.Manager;
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

            CreateMap<MenuItem, ManagerMenuDTO>();

            CreateMap<Combo, ManagerComboDTO>();
            CreateMap<ComboItem, ManagerComboItemDTO>().ForMember(dest => dest.ManagerMenuItem,
                      opt => opt.MapFrom(src => src.MenuItem)); ;
            CreateMap<MenuCategory, ManagerCategoryDTO>();
            CreateMap<ManagerCategoryDTO, MenuCategory>();

            CreateMap<InventoryBatch, InventoryIngredientWithBatchDTO>()
                .ForMember(dest => dest.IngredientId, opt => opt.MapFrom(src => src.Ingredient.IngredientId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Ingredient.Name))
                .ForMember(dest => dest.Unit, opt => opt.MapFrom(src => src.Ingredient.Unit))
                .ForMember(dest => dest.ReorderLevel, opt => opt.MapFrom(src => src.Ingredient.ReorderLevel));
        }
    }
}
