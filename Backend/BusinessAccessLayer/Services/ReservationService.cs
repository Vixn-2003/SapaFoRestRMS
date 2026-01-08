using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static DataAccessLayer.Repositories.ReservationRepository;

namespace BusinessAccessLayer.Services
{
    public class ReservationService : IReservationService
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICustomerRepository _customerRepository; // thêm repo Customer
        private readonly IConfiguration _configuration;

        private const decimal DEPOSIT_PER_GUEST = 50000m;

        public ReservationService(
            IReservationRepository reservationRepository,
            IUserRepository userRepository,
            ICustomerRepository customerRepository,
            IConfiguration configuration) // inject
        {
            _reservationRepository = reservationRepository;
            _userRepository = userRepository;
            _customerRepository = customerRepository;
            _configuration = configuration;
        }

        public async Task<bool> HasExistingReservationAsync(string phone, DateTime date, string timeSlot)
        {
            var existingReservations = await _reservationRepository
                .GetReservationsByPhoneAndDateAndSlotAsync(phone, date, timeSlot);

            if (existingReservations == null)
                return false;
            //check PENDING hoặc CONFIRMED
            return existingReservations.Any(r =>
       r.Status == "Pending" ||
       r.Status == "Confirmed"
   );
        }

        public async Task<Reservation?> CreateReservationAsync(ReservationCreateDto dto)
        {
            // Lấy user theo phone
            var user = await _userRepository.GetByPhoneAsync(dto.Phone);
            if (user == null)
            {
                //  FIX: Set Status=0 (active) để customer có thể đăng nhập ngay sau khi đặt bàn
                var defaultAvatar = _configuration["CloudinarySettings:DefaultAvatarUrl"]
                    ?? "/images/default-avatar.jpg";

                user = new User
                {
                    FullName = dto.CustomerName,
                    Email = $"customer_{dto.Phone}@gmail.com",
                    PasswordHash = "666666", // TODO: hash thật
                    Phone = dto.Phone,
                    RoleId = 5,
                    Status = 0, // 0 = Active, 1 = Inactive
                    AvatarUrl = defaultAvatar // use Cloudinary default avatar URL if configured
                };
                user = await _userRepository.CreateAsync(user);
            }

            // Lấy Customer
            var customer = await _customerRepository.GetByUserIdAsync(user.UserId);
            if (customer == null)
            {
                customer = new Customer { UserId = user.UserId };
                customer = await _customerRepository.CreateAsync(customer);
            }

            string GetTimeSlot(DateTime reservationTime)
            {
                var hour = reservationTime.Hour;
                if (hour >= 6 && hour < 10) return "Ca sáng";
                if (hour >= 10 && hour < 14) return "Ca trưa";
                return "Ca tối";
            }

            var fullDateTime = dto.ReservationDate.Date + dto.ReservationTime.TimeOfDay;
            var timeSlot = GetTimeSlot(fullDateTime);

            // Kiểm tra trùng đơn theo phone + ngày + ca
            var existingReservations = await _reservationRepository
    .GetReservationsByPhoneAndDateAndSlotAsync(dto.Phone, dto.ReservationDate.Date, timeSlot);

            if (existingReservations != null &&
                existingReservations.Any(r => r.Status == "Pending" || r.Status == "Confirmed"))
            {
                return null;
            }


            decimal depositAmount = dto.NumberOfGuests * DEPOSIT_PER_GUEST;

            var reservation = new Reservation
            {
                CustomerNameReservation = dto.CustomerName,
                CustomerId = customer.CustomerId,
                ReservationDate = dto.ReservationDate.Date,
                ReservationTime = fullDateTime,
                TimeSlot = timeSlot,
                NumberOfGuests = dto.NumberOfGuests,
                Notes = dto.Notes,
                Status = "Pending",
                RequireDeposit = true,
                DepositAmount = depositAmount,
                TotalDepositPaid = 0,
                DepositPaid = false
            };

            return await _reservationRepository.CreateAsync(reservation);
        }

        public async Task AddDepositAsync(int reservationId, ReservationDeposit deposit)
        {
            var reservation = await _reservationRepository.GetByIdAsync(reservationId)
                              ?? throw new Exception("Reservation not found");

            reservation.ReservationDeposits.Add(deposit);
            reservation.TotalDepositPaid = (reservation.TotalDepositPaid ?? 0) + deposit.Amount;
            reservation.DepositPaid = reservation.TotalDepositPaid >= reservation.DepositAmount;

            await _reservationRepository.UpdateAsync(reservation);
        }

        public async Task UpdateReservationDepositStatusAsync(Reservation reservation)
        {
            await _reservationRepository.UpdateAsync(reservation);
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
                    r.DepositAmount,
                    r.TotalDepositPaid,
                    r.DepositPaid,
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

        public async Task<List<BookedTableDetailDto>> GetBookedTableDetailsAsync(DateTime date, string slot)
        {
            return await _reservationRepository.GetBookedTableDetailsAsync(date, slot);
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

            return combos
                .OrderBy(c => c.Count)
                .ThenBy(c => c.Sum(t => t.Capacity) - guests)
                .Take(maxSuggestions)
                .ToList();
        }

        public async Task<object> AssignTablesAsync(AssignTableDto dto)
        {
            if (dto.TableIds == null || !dto.TableIds.Any())
                throw new Exception("Bạn phải chọn ít nhất 1 bàn trước khi xác nhận.");

            // ❌ BỎ validate yêu cầu đặt cọc
            // if (dto.RequireDeposit && (!dto.DepositAmount.HasValue || dto.DepositAmount.Value <= 0))
            //     throw new Exception("Bạn phải nhập số tiền đặt cọc hợp lệ.");

            var reservation = await _reservationRepository.GetReservationByIdAsync(dto.ReservationId);
            if (reservation == null)
                throw new Exception("Reservation không tồn tại.");

            //  CHẶN gán bàn nếu đã Confirmed hoặc Cancelled (bắt buộc Reset trước)
            if (reservation.Status == "Cancelled" || reservation.Status == "Confirmed")
                throw new Exception("Đơn đang ở trạng thái 'Cancelled' hoặc 'Confirmed' nên không thể gán bàn. Vui lòng Reset đơn về 'Pending' trước khi gán lại.");

            //  (khuyến nghị) Chỉ cho phép gán bàn khi Pending
            if (reservation.Status != "Pending")
                throw new Exception("Chỉ có thể gán bàn khi đơn ở trạng thái 'Pending'. Vui lòng Reset trước khi gán lại.");

            // Check conflict
            var conflict = (await _reservationRepository
                                .GetBookedTableIdsAsync(reservation.ReservationDate, reservation.TimeSlot))
                           .Any(id => dto.TableIds.Contains(id)
                                      && !reservation.ReservationTables.Any(rt => rt.TableId == id));

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

            // ❌ KHÔNG set lại RequireDeposit / DepositAmount / DepositPaid ở đây nữa
            // reservation.RequireDeposit = dto.RequireDeposit;
            // reservation.DepositAmount = dto.RequireDeposit && dto.DepositAmount.HasValue ? dto.DepositAmount.Value : null;
            // reservation.DepositPaid = dto.ConfirmBooking && dto.RequireDeposit && dto.DepositAmount.HasValue;

            reservation.Status = dto.ConfirmBooking ? "Confirmed" : "Pending";
            reservation.StaffId = dto.StaffId;

            await _reservationRepository.SaveChangesAsync();

            return new
            {
                reservation.ReservationId,
                reservation.Status,
                // vẫn trả về thông tin đặt cọc hiện tại nếu nó đã tồn tại
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
            
            reservation.StaffId = null;

            await _reservationRepository.SaveChangesAsync();

            return new
            {
                reservation.ReservationId,
                reservation.Status,
                TableIds = reservation.ReservationTables.Select(rt => rt.TableId).ToList()
            };
        }

        public async Task<object> CancelReservationAsync(int reservationId, bool refund)
        {
            var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId);
            if (reservation == null)
                throw new Exception("Không tìm thấy đơn đặt bàn.");

            // Xử lý hoàn cọc nếu refund = true
            if (refund && reservation.DepositPaid && reservation.DepositAmount.HasValue)
            {
                // đánh dấu đã hoàn cọc
                reservation.DepositPaid = false;
                // có thể thêm logic hoàn tiền/email ở đây
            }

            // Nếu đơn chưa hủy, hủy luôn
            if (reservation.Status != "Cancelled")
            {
                reservation.Status = "Cancelled";
                reservation.ReservationTables.Clear(); // giải phóng bàn
            }

            await _reservationRepository.SaveChangesAsync();

            return new
            {
                reservation.ReservationId,
                reservation.Status,
                reservation.DepositAmount,
                reservation.DepositPaid,
                Refunded = refund
            };
        }

        // Lấy danh sách đặt bàn của khách hàng
        public async Task<object> GetReservationsByCustomerAsync(int customerId)
        {
            var reservations = await _reservationRepository.GetReservationsByCustomerAsync(customerId);

            return reservations.Select(r => new
            {
                r.ReservationId,
                r.CustomerNameReservation,
                r.ReservationDate,
                r.ReservationTime,
                r.TimeSlot,
                r.NumberOfGuests,
                r.Status,
                r.Notes,
                r.DepositAmount,
                r.DepositPaid
            }).ToList();
        }

        // Cập nhật đặt bàn khi trạng thái là Pending
        public async Task<object> UpdateReservationAsync(int reservationId, ReservationUpdateDto dto)
        {
            // 1. Lấy thông tin đặt bàn
            var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId);
            if (reservation == null)
                throw new Exception("Không tìm thấy đơn đặt bàn.");

            // 2. Chỉ cho phép chỉnh sửa khi trạng thái là Pending
            if (reservation.Status != "Pending")
                throw new Exception("Chỉ có thể chỉnh sửa đơn ở trạng thái 'Pending'.");

            // 3. Kiểm tra dữ liệu hợp lệ
            if (dto.NumberOfGuests <= 0)
                throw new Exception("Số lượng khách phải lớn hơn 0.");

            if (dto.ReservationDate == DateTime.MinValue)
                throw new Exception("Vui lòng chọn ngày đặt bàn hợp lệ.");

            if (dto.ReservationTime == DateTime.MinValue)
                throw new Exception("Vui lòng chọn giờ đặt bàn hợp lệ.");

            // 4. Kiểm tra ngày & giờ đặt
            DateTime currentDate = DateTime.Now.Date;
            DateTime currentDateTime = DateTime.Now;
            DateTime reservationDate = dto.ReservationDate.Date;
            DateTime combinedTime = reservationDate + dto.ReservationTime.TimeOfDay;

            if (reservationDate < currentDate)
                throw new Exception("Ngày đặt không được nhỏ hơn ngày hiện tại.");

            // Nếu là hôm nay thì phải đặt sau thời điểm hiện tại
            if (reservationDate == currentDate && combinedTime <= currentDateTime)
                throw new Exception("Thời gian đặt bàn hôm nay phải lớn hơn thời điểm hiện tại.");

            // Cập nhật thông tin đặt bàn
            reservation.ReservationDate = reservationDate;
            reservation.ReservationTime = combinedTime;
            reservation.NumberOfGuests = dto.NumberOfGuests;
            reservation.Notes = dto.Notes;

            // Cập nhật lại TimeSlot theo giờ
            string GetTimeSlot(DateTime time)
            {
                int hour = time.Hour;
                if (hour >= 6 && hour < 10) return "Ca sáng";
                if (hour >= 10 && hour < 14) return "Ca trưa";
                return "Ca tối";
            }
            reservation.TimeSlot = GetTimeSlot(reservation.ReservationTime);

            // Lưu thay đổi
            await _reservationRepository.SaveChangesAsync();

            // Trả về dữ liệu cập nhật
            return new
            {
                reservation.ReservationId,
                reservation.ReservationDate,
                reservation.ReservationTime,
                reservation.TimeSlot,
                reservation.NumberOfGuests,
                reservation.Notes,
                reservation.Status
            };
        }

        // Hủy đặt bàn do khách hàng thực hiện
        public async Task<object> CancelReservationByCustomerAsync(int reservationId)
        {
            var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId);
            if (reservation == null)
                throw new Exception("Không tìm thấy đơn đặt bàn.");

            if (reservation.Status != "Pending" && reservation.Status != "Confirmed")
                throw new Exception("Chỉ có thể hủy đơn ở trạng thái 'Pending' hoặc 'Confirmed'.");

            reservation.Status = "Cancelled";
            reservation.ReservationTables.Clear();

            await _reservationRepository.SaveChangesAsync();

            return new
            {
                reservation.ReservationId,
                reservation.Status
            };
        }

        public Task<int> GetPendingCountAsync()
        {
            return _reservationRepository.GetPendingCountAsync();
        }

        public async Task<int?> GetActiveReservationIdByTableAsync(int tableId)
        {
            var reservation = await _reservationRepository.GetActiveByTableIdAsync(tableId);

            if (reservation == null) return null;

            return reservation.ReservationId; 
        }
    }
}
