using AutoMapper;
using BusinessAccessLayer.DTOs.CounterStaff;
using DomainAccessLayer.Models;

namespace BusinessAccessLayer.Mapping
{
    /// <summary>
    /// AutoMapper Profile cho Counter Staff module
    /// </summary>
    public class CounterStaffMappingProfile : Profile
    {
        public CounterStaffMappingProfile()
        {
            // Order -> OrderListItemDto
            CreateMap<Order, OrderListItemDto>()
                .ForMember(dest => dest.OrderCode, opt => opt.MapFrom(src => $"RMS{src.OrderId:D6}"))
                .ForMember(dest => dest.TableNumber, opt => opt.MapFrom(src =>
                    src.Reservation != null && src.Reservation.ReservationTables != null && src.Reservation.ReservationTables.Any()
                        ? string.Join(", ", src.Reservation.ReservationTables.Select(rt => rt.Table != null ? rt.Table.TableNumber : "N/A"))
                        : "N/A"))
                .ForMember(dest => dest.IsWaiterConfirmed, opt => opt.MapFrom(src => src.ConfirmedAt.HasValue))
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src =>
                    src.Customer != null && src.Customer.User != null
                        ? src.Customer.User.FullName
                        : src.Reservation != null && src.Reservation.Customer != null && src.Reservation.Customer.User != null
                            ? src.Reservation.Customer.User.FullName
                            : "Unknown"))
                .ForMember(dest => dest.NumberOfGuests, opt => opt.MapFrom(src =>
                    src.Reservation != null ? src.Reservation.NumberOfGuests : (int?)null));

            // Transaction -> TransactionHistoryDto
            CreateMap<Transaction, TransactionHistoryDto>()
                .ForMember(dest => dest.OrderCode, opt => opt.MapFrom(src => $"RMS{src.OrderId:D6}"))
                .ForMember(dest => dest.TableNumber, opt => opt.MapFrom(src =>
                    src.Order != null && src.Order.Reservation != null && 
                    src.Order.Reservation.ReservationTables != null && 
                    src.Order.Reservation.ReservationTables.Any()
                        ? string.Join(", ", src.Order.Reservation.ReservationTables
                            .Select(rt => rt.Table != null ? rt.Table.TableNumber : "N/A"))
                        : "N/A"))
                .ForMember(dest => dest.CashierName, opt => opt.MapFrom(src =>
                    src.ConfirmedByUser != null ? src.ConfirmedByUser.FullName : "System"));

            // Transaction -> TransactionExcelDto
            CreateMap<Transaction, TransactionExcelDto>()
                .ForMember(dest => dest.OrderCode, opt => opt.MapFrom(src => $"RMS{src.OrderId:D6}"))
                .ForMember(dest => dest.TableNumber, opt => opt.MapFrom(src =>
                    src.Order != null && src.Order.Reservation != null && 
                    src.Order.Reservation.ReservationTables != null && 
                    src.Order.Reservation.ReservationTables.Any()
                        ? string.Join(", ", src.Order.Reservation.ReservationTables
                            .Select(rt => rt.Table != null ? rt.Table.TableNumber : "N/A"))
                        : "N/A"))
                .ForMember(dest => dest.CashierName, opt => opt.MapFrom(src =>
                    src.ConfirmedByUser != null ? src.ConfirmedByUser.FullName : "System"));
        }
    }
}

