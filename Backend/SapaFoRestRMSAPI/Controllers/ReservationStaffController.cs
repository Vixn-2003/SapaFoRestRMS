using DataAccessLayer.Dbcontext;
using DomainAccessLayer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SapaFoRestRMSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationStaffController : ControllerBase
    {
        private readonly SapaFoRestRmsContext _context;

        public ReservationStaffController(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        [HttpGet("reservations/pending-confirmed")]
        public async Task<IActionResult> GetPendingAndConfirmedReservations()
        {
            var reservations = await _context.Reservations
                .Include(r => r.Customer)
                    .ThenInclude(c => c.User)
                .Where(r => r.Status == "Pending" || r.Status == "Confirmed")
                .Select(r => new
                {
                    r.ReservationId,
                    CustomerName = r.CustomerNameReservation,
                    CustomerPhone = r.Customer.User.Phone,
                    r.ReservationDate,
                    r.ReservationTime,
                    r.TimeSlot,
                    r.NumberOfGuests,
                    r.Status,
                    TableIds = r.ReservationTables.Select(rt => rt.TableId).ToList()
                })
                .ToListAsync();

            return Ok(reservations);
        }

        [HttpGet("tables/by-area-all")]
        public async Task<IActionResult> GetAllTablesGroupedByArea()
        {
            var areasWithTables = await _context.Areas
                .Include(a => a.Tables)
                .Select(a => new
                {
                    a.AreaId,
                    a.AreaName,
                    Tables = a.Tables.Select(t => new
                    {
                        t.TableId,
                        TableName = t.TableNumber,
                        t.Capacity
                    }).ToList()
                })
                .ToListAsync();

            return Ok(areasWithTables);
        }

        [HttpGet("tables/booked")]
        public async Task<IActionResult> GetBookedTables(DateTime reservationDate, string timeSlot)
        {
            var bookedTableIds = await _context.ReservationTables
                .Where(rt => rt.Reservation.ReservationDate.Date == reservationDate.Date
                          && rt.Reservation.TimeSlot == timeSlot
                          && rt.Reservation.Status != "Cancelled")
                .Select(rt => rt.TableId)
                .ToListAsync();

            return Ok(new { BookedTableIds = bookedTableIds });
        }

        [HttpGet("tables/suggest-by-areas")]
        public async Task<IActionResult> SuggestTablesByAreas(
            DateTime reservationDate,
            string timeSlot,
            int numberOfGuests,
            int? currentReservationId = null)
        {
            var allTables = await _context.Tables
                .Include(t => t.Area)
                .Select(t => new TableDto
                {
                    TableId = t.TableId,
                    TableName = t.TableNumber,
                    Capacity = t.Capacity,
                    AreaId = t.AreaId,
                    AreaName = t.Area.AreaName
                })
                .ToListAsync();

            var bookedTableIds = await _context.ReservationTables
                .Where(rt => rt.Reservation.ReservationDate.Date == reservationDate.Date
                          && rt.Reservation.TimeSlot == timeSlot
                          && rt.Reservation.Status != "Cancelled")
                .Select(rt => rt.TableId)
                .ToListAsync();

            var availableTables = allTables
                .Where(t => !bookedTableIds.Contains(t.TableId))
                .ToList();

            var areaSuggestions = availableTables
     .GroupBy(t => new { t.AreaId, t.AreaName })
     .Select(g => new
     {
         g.Key.AreaId,
         g.Key.AreaName,
         AllAvailableTables = g.ToList(),
         SuggestedSingleTables = g.Where(t => t.Capacity >= numberOfGuests)
                                  .OrderBy(t => t.Capacity)
                                  .ToList(),
         SuggestedCombos = GetSmartCombos(g.ToList(), numberOfGuests, 1)
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

            return Ok(new { Areas = areaSuggestions });
        }
        private List<List<TableDto>> GetSmartCombos(List<TableDto> availableTables, int guests, int maxSuggestions = 1)
        {
            var combos = new List<List<TableDto>>();

            // 1️⃣ Ưu tiên bàn đơn đủ sức chứa
            var singleTable = availableTables.FirstOrDefault(t => t.Capacity >= guests);
            if (singleTable != null)
            {
                combos.Add(new List<TableDto> { singleTable });
                return combos.Take(maxSuggestions).ToList();
            }

            // 2️⃣ Backtrack tạo tất cả combo khả thi
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

            var sortedTables = availableTables.OrderByDescending(t => t.Capacity).ToList();
            Backtrack(new List<TableDto>(), 0, 0);

            // 3️⃣ Sắp xếp theo số bàn ít nhất, sau đó theo tổng dư chỗ nhỏ nhất
            var smartCombos = combos
                .OrderBy(c => c.Count)  // ít bàn nhất
                .ThenBy(c => c.Sum(t => t.Capacity) - guests) // dư chỗ ít nhất
                .Take(maxSuggestions) // lấy 1-2 combo tối ưu
                .ToList();

            return smartCombos;
        }

        //private List<List<TableDto>> GetTableCombos(List<TableDto> availableTables, int guests)
        //{
        //    var combos = new List<List<TableDto>>();
        //    var tables = availableTables.OrderBy(t => t.Capacity).ToList();

        //    void Backtrack(List<TableDto> current, int index, int sum)
        //    {
        //        if (sum >= guests)
        //        {
        //            combos.Add(new List<TableDto>(current));
        //            return;
        //        }

        //        for (int i = index; i < tables.Count; i++)
        //        {
        //            current.Add(tables[i]);
        //            Backtrack(current, i + 1, sum + tables[i].Capacity);
        //            current.RemoveAt(current.Count - 1);
        //        }
        //    }

        //    Backtrack(new List<TableDto>(), 0, 0);

        //    return combos.OrderBy(c => c.Count).ToList(); // ưu tiên ít bàn nhất
        //}
        [HttpPost("assign-tables")]
        public async Task<IActionResult> AssignTables([FromBody] AssignTableDto dto)
        {
            var reservation = await _context.Reservations
                .Include(r => r.ReservationTables)
                .FirstOrDefaultAsync(r => r.ReservationId == dto.ReservationId);

            if (reservation == null)
                return NotFound("Reservation không tồn tại.");

            // Kiểm tra xung đột bàn
            var conflict = await _context.ReservationTables
                .Where(rt => dto.TableIds.Contains(rt.TableId)
                          && rt.Reservation.ReservationDate.Date == reservation.ReservationDate.Date
                          && rt.Reservation.TimeSlot == reservation.TimeSlot
                          && rt.ReservationId != reservation.ReservationId)
                .AnyAsync();

            if (conflict)
                return BadRequest("Một hoặc nhiều bàn đã bị đặt trước cho slot này.");

            // Xóa bàn cũ
            reservation.ReservationTables.Clear();

            // Gán bàn mới
            foreach (var tableId in dto.TableIds)
            {
                reservation.ReservationTables.Add(new ReservationTable
                {
                    ReservationId = reservation.ReservationId,
                    TableId = tableId
                });
            }

            // Xử lý deposit
            reservation.RequireDeposit = dto.RequireDeposit;
            reservation.DepositAmount = dto.RequireDeposit && dto.DepositAmount.HasValue ? dto.DepositAmount.Value : null;
            reservation.DepositPaid = dto.ConfirmBooking && dto.RequireDeposit && dto.DepositAmount.HasValue;

            // Cập nhật trạng thái
            reservation.Status = dto.ConfirmBooking ? "Confirmed" : "Pending";
            reservation.StaffId = dto.StaffId;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                reservation.ReservationId,
                reservation.Status,
                reservation.DepositAmount,
                reservation.DepositPaid,
                reservation.StaffId,
                TableIds = reservation.ReservationTables.Select(rt => rt.TableId).ToList()
            });
        }
        [HttpPost("reset-tables/{reservationId}")]
        public async Task<IActionResult> ResetTables(int reservationId)
        {
            // Lấy reservation kèm danh sách bàn đã gán
            var reservation = await _context.Reservations
                .Include(r => r.ReservationTables)
                .FirstOrDefaultAsync(r => r.ReservationId == reservationId);

            if (reservation == null)
                return NotFound("Reservation không tồn tại.");

            // Xóa tất cả bàn đã gán
            reservation.ReservationTables.Clear();

            // Reset trạng thái về Pending
            reservation.Status = "Pending";
            reservation.RequireDeposit = false;
            reservation.DepositAmount = null;
            reservation.DepositPaid = false;
            reservation.StaffId = null;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                reservation.ReservationId,
                reservation.Status,
                TableIds = reservation.ReservationTables.Select(rt => rt.TableId).ToList()
            });
        }

        public class TableDto
        {
            public int TableId { get; set; }
            public string TableName { get; set; } = null!;
            public int Capacity { get; set; }
            public int AreaId { get; set; }
            public string AreaName { get; set; } = null!;
        }
        public class AssignTableDto
        {
            public int ReservationId { get; set; }
            public List<int> TableIds { get; set; } = new();
            public bool RequireDeposit { get; set; }
            public decimal? DepositAmount { get; set; }
            public int StaffId { get; set; }
            public bool ConfirmBooking { get; set; } = false;
        }
    }
}
