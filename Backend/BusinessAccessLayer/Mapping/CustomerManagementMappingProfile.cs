using AutoMapper;
using BusinessAccessLayer.DTOs.CustomerManagement;
using DomainAccessLayer.Models;

namespace BusinessAccessLayer.Mapping
{
    /// <summary>
    /// AutoMapper profile cho Customer Management Module
    /// </summary>
    public class CustomerManagementMappingProfile : Profile
    {
        public CustomerManagementMappingProfile()
        {
            // Customer -> CustomerListItemDto
            CreateMap<Customer, CustomerListItemDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : "N/A"))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.User != null ? src.User.Phone : null))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User != null ? src.User.Email : null))
                .ForMember(dest => dest.TotalSpending, opt => opt.Ignore()) // Calculated in repository
                .ForMember(dest => dest.TotalVisits, opt => opt.Ignore()) // Calculated in repository
                .ForMember(dest => dest.LastVisit, opt => opt.Ignore()) // Calculated in repository
                .ForMember(dest => dest.AverageSpendPerVisit, opt => opt.Ignore()); // Calculated in repository

            // Customer -> CustomerDetailDto
            CreateMap<Customer, CustomerDetailDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : "N/A"))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.User != null ? src.User.Phone : null))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User != null ? src.User.Email : null))
                .ForMember(dest => dest.JoinDate, opt => opt.MapFrom(src => src.User != null ? src.User.CreatedAt : null))
                .ForMember(dest => dest.TotalSpending, opt => opt.Ignore()) // Calculated in service
                .ForMember(dest => dest.TotalVisits, opt => opt.Ignore()) // Calculated in service
                .ForMember(dest => dest.AverageSpendPerVisit, opt => opt.Ignore()) // Calculated in service
                .ForMember(dest => dest.LastVisit, opt => opt.Ignore()) // Calculated in service
                .ForMember(dest => dest.FavoriteDishes, opt => opt.Ignore()) // Calculated in service
                .ForMember(dest => dest.OrderHistory, opt => opt.Ignore()) // Calculated in service
                .ForMember(dest => dest.SpendingTrend, opt => opt.Ignore()); // Calculated in service

            // Order -> CustomerOrderSummaryDto
            CreateMap<Order, CustomerOrderSummaryDto>()
                .ForMember(dest => dest.OrderDate, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.NumberOfItems, opt => opt.Ignore()) // Calculated in service
                .ForMember(dest => dest.PaymentId, opt => opt.Ignore()); // Calculated in service
            
            // Note: FavoriteDishDto is calculated manually in service (không cần mapping)
        }
    }
}

