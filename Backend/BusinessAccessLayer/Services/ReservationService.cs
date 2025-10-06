using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using System;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    public class ReservationService : IReservationService
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICustomerRepository _customerRepository; // thêm repo Customer

        public ReservationService(
            IReservationRepository reservationRepository,
            IUserRepository userRepository,
            ICustomerRepository customerRepository) // inject
        {
            _reservationRepository = reservationRepository;
            _userRepository = userRepository;
            _customerRepository = customerRepository;
        }

        public async Task<Reservation> CreateReservationAsync(ReservationCreateDto dto)
        {
            // Lấy user theo phone qua repo
            var user = await _userRepository.GetByPhoneAsync(dto.Phone);
            if (user == null)
            {
                user = new User
                {
                    FullName = dto.CustomerName,
                    PasswordHash = "666666", 
                    Phone = dto.Phone,
                    RoleId = 2,
                    Email = $"{dto.CustomerName.Replace(" ", "").ToLower()}@gmail.com"
                };
                user = await _userRepository.CreateAsync(user);
            }

            // Lấy Customer của user nếu có
            var customer = await _customerRepository.GetByUserIdAsync(user.UserId);
            if (customer == null)
            {
                customer = new Customer { UserId = user.UserId };
                customer = await _customerRepository.CreateAsync(customer);
            }

            // Tự tính TimeSlot từ ReservationTime
            string GetTimeSlot(DateTime reservationTime)
            {
                var hour = reservationTime.Hour;
                if (hour >= 6 && hour < 10) return "Ca sáng";
                if (hour >= 10 && hour < 14) return "Ca trưa";
                return "Ca tối";
            }

            var reservation = new Reservation
            {
                CustomerNameReservation = dto.CustomerName,
                CustomerId = customer.CustomerId,
                ReservationDate = dto.ReservationDate.Date, 
                ReservationTime = dto.ReservationDate.Date + dto.ReservationTime.TimeOfDay, 
                TimeSlot = GetTimeSlot(dto.ReservationDate.Date + dto.ReservationTime.TimeOfDay),
                NumberOfGuests = dto.NumberOfGuests,
                Notes = dto.Notes,
                Status = "Pending"
            };

            return await _reservationRepository.CreateAsync(reservation);
        }
    }
}
