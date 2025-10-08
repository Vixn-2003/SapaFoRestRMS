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
                    Email = $"customer_{dto.Phone}@gmail.com",
                    PasswordHash = "666666",
                    Phone = dto.Phone,
                    RoleId = 2
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
                ReservationDate = dto.ReservationDate.Date, // chỉ ngày
                ReservationTime = dto.ReservationDate.Date + dto.ReservationTime.TimeOfDay,
                TimeSlot = GetTimeSlot(dto.ReservationDate.Date + dto.ReservationTime.TimeOfDay),
                NumberOfGuests = dto.NumberOfGuests,
                Notes = dto.Notes,
                Status = "Pending"
            };

            return await _reservationRepository.CreateAsync(reservation);
        }
        public async Task<object> GetPendingAndConfirmedReservationsAsync(
     string? status = null,
     DateTime? date = null,
     string? customerName = null,
     string? phone = null,
     string? timeSlot = null,
     int page = 1,
     int pageSize = 10)
        {
            var (reservations, totalCount) = await _reservationRepository
                .GetPendingAndConfirmedReservationsAsync(status, date, customerName, phone, timeSlot, page, pageSize);

            var result = new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Data = reservations.Select(r => new
                {
                    r.ReservationId,
                    CustomerName = r.CustomerNameReservation,
                    CustomerPhone = r.Customer?.User?.Phone,
                    r.ReservationDate,
                    r.ReservationTime,
                    r.TimeSlot,
                    r.NumberOfGuests,
                    r.Status,
                    TableIds = r.ReservationTables.Select(rt => rt.TableId).ToList()
                }).ToList()
            };

            return result;
        }
        public async Task<object?> GetReservationDetailAsync(int reservationId)
        {
            return await _reservationRepository.GetReservationDetailAsync(reservationId);
        }


        public async Task<object> GetAllTablesGroupedByAreaAsync()
        {
            var areas = await _reservationRepository.GetAllAreasWithTablesAsync();
            return areas.Select(a => new
            {
                a.AreaId,
                a.AreaName,
                Tables = a.Tables.Select(t => new
                {
                    t.TableId,
                    TableName = t.TableNumber,
                    t.Capacity
                }).ToList()
            });
        }

        public async Task<List<int>> GetBookedTableIdsAsync(DateTime date, string slot)
        {
            return await _reservationRepository.GetBookedTableIdsAsync(date, slot);
        }

        public async Task<object> SuggestTablesByAreasAsync(DateTime date, string slot, int guests, int? currentReservationId = null)
        {
            var allTables = (await _reservationRepository.GetAllAreasWithTablesAsync())
                .SelectMany(a => a.Tables.Select(t => new TableDto
                {
                    TableId = t.TableId,
                    TableName = t.TableNumber,
                    Capacity = t.Capacity,
                    AreaId = t.AreaId,
                    AreaName = t.Area.AreaName
                }))
                .ToList();

            var bookedIds = await GetBookedTableIdsAsync(date, slot);
            var available = allTables.Where(t => !bookedIds.Contains(t.TableId)).ToList();

            var areaSuggestions = available
                .GroupBy(t => new { t.AreaId, t.AreaName })
                .Select(g => new
                {
                    g.Key.AreaId,
                    g.Key.AreaName,
                    AllAvailableTables = g.ToList(),
                    SuggestedSingleTables = g.Where(t => t.Capacity >= guests).OrderBy(t => t.Capacity).ToList(),
                    SuggestedCombos = GetSmartCombos(g.ToList(), guests, 1)
                        .Select(c => c.Select(t => new TableDto
                        {
                            TableId = t.TableId,
                            TableName = t.TableName,
                            Capacity = t.Capacity,
                            AreaId = t.AreaId,
                            AreaName = t.AreaName
                        }).ToList())
                        .ToList()
                })
                .ToList();

            return new { Areas = areaSuggestions };
        }

        private List<List<TableDto>> GetSmartCombos(List<TableDto> availableTables, int guests, int maxSuggestions = 1)
        {
            var combos = new List<List<TableDto>>();

            var single = availableTables.FirstOrDefault(t => t.Capacity >= guests);
            if (single != null)
            {
                combos.Add(new List<TableDto> { single });
                return combos.Take(maxSuggestions).ToList();
            }

            void Backtrack(List<TableDto> current, int index, int sum)
            {
                if (sum >= guests)
                {
                    combos.Add(new List<TableDto>(current));
                    return;
                }

                for (int i = index; i < availableTables.Count; i++)
                {
                    current.Add(availableTables[i]);
                    Backtrack(current, i + 1, sum + availableTables[i].Capacity);
                    current.RemoveAt(current.Count - 1);
                }
            }

            var sorted = availableTables.OrderByDescending(t => t.Capacity).ToList();
            Backtrack(new List<TableDto>(), 0, 0);

            return combos.OrderBy(c => c.Count).ThenBy(c => c.Sum(t => t.Capacity) - guests)
                         .Take(maxSuggestions).ToList();
        }

        public async Task<object> AssignTablesAsync(AssignTableDto dto)
        {
            if (dto.TableIds == null || !dto.TableIds.Any())
                throw new Exception("Bạn phải chọn ít nhất 1 bàn trước khi xác nhận.");

            if (dto.RequireDeposit && (!dto.DepositAmount.HasValue || dto.DepositAmount.Value <= 0))
                throw new Exception("Bạn phải nhập số tiền đặt cọc hợp lệ.");

            var reservation = await _reservationRepository.GetReservationByIdAsync(dto.ReservationId);
            if (reservation == null)
                throw new Exception("Reservation không tồn tại.");

            // Check conflict
            var conflict = (await _reservationRepository.GetBookedTableIdsAsync(reservation.ReservationDate, reservation.TimeSlot))
                           .Any(id => dto.TableIds.Contains(id) && !reservation.ReservationTables.Any(rt => rt.TableId == id));

            if (conflict)
                throw new Exception("Một hoặc nhiều bàn đã bị đặt trước cho slot này.");

            // Xóa bàn cũ
            reservation.ReservationTables.Clear();

            foreach (var id in dto.TableIds)
            {
                reservation.ReservationTables.Add(new ReservationTable
                {
                    ReservationId = reservation.ReservationId,
                    TableId = id
                });
            }

            reservation.RequireDeposit = dto.RequireDeposit;
            reservation.DepositAmount = dto.RequireDeposit && dto.DepositAmount.HasValue ? dto.DepositAmount.Value : null;
            reservation.DepositPaid = dto.ConfirmBooking && dto.RequireDeposit && dto.DepositAmount.HasValue;
            reservation.Status = dto.ConfirmBooking ? "Confirmed" : "Pending";
            reservation.StaffId = dto.StaffId;

            await _reservationRepository.SaveChangesAsync();

            return new
            {
                reservation.ReservationId,
                reservation.Status,
                reservation.DepositAmount,
                reservation.DepositPaid,
                reservation.StaffId,
                TableIds = reservation.ReservationTables.Select(rt => rt.TableId).ToList()
            };
        }

        public async Task<object> ResetTablesAsync(int reservationId)
        {
            var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId);
            if (reservation == null)
                throw new Exception("Reservation không tồn tại.");

            reservation.ReservationTables.Clear();
            reservation.Status = "Pending";
            reservation.RequireDeposit = false;
            reservation.DepositAmount = null;
            reservation.DepositPaid = false;
            reservation.StaffId = null;

            await _reservationRepository.SaveChangesAsync();

            return new
            {
                reservation.ReservationId,
                reservation.Status,
                TableIds = reservation.ReservationTables.Select(rt => rt.TableId).ToList()
            };
        }
    }
}
