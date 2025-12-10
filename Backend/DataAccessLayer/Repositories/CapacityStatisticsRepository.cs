using DataAccessLayer.Dbcontext;
using DataAccessLayer.DTOs;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories
{
    public class CapacityStatisticsRepository : ICapacityStatisticsRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public CapacityStatisticsRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        public async Task<DayCapacitySummaryDto> GetDayCapacityAsync(DateTime date)
        {
            // 1. Tổng sức chứa nhà hàng = tổng Capacity của tất cả bàn
            var totalRestaurantCapacity = await _context.Tables
                .SumAsync(t => t.Capacity);

            // 2. Lấy các reservation trong ngày (Pending + Confirmed)
            //    và "dỡ phẳng" sang các dòng ReservationTable + Table.Capacity
            var reservationsInDay = _context.Reservations
                .Where(r => r.ReservationDate.Date == date.Date
                            )
                .SelectMany(r => r.ReservationTables.Select(rt => new
                {
                    r.TimeSlot,
                    TableCapacity = rt.Table.Capacity
                }));

            // 3. Gom nhóm theo TimeSlot, tính tổng capacity của các bàn đã xếp trong ca đó
            var perTimeSlotRaw = await reservationsInDay
                .GroupBy(x => x.TimeSlot)
                .Select(g => new
                {
                    TimeSlot = g.Key,
                    ReservedCapacity = g.Sum(x => x.TableCapacity)   // tổng chỗ đã xếp (theo capacity bàn)
                })
                .ToListAsync();

            // 4. Map sang DTO cho từng ca
            var timeSlotDtos = perTimeSlotRaw
                .Select(x => new TimeSlotCapacityDto
                {
                    TimeSlot = x.TimeSlot,
                    TotalCapacity = totalRestaurantCapacity,                 // tổng sức chứa nhà hàng
                    ReservedGuests = x.ReservedCapacity,                     // thực chất là tổng chỗ đã xếp
                    RemainingCapacity = totalRestaurantCapacity - x.ReservedCapacity
                })
                .ToList();

            // 5. Tính tổng chỗ đã xếp trong cả ngày (cộng tất cả ca)
            var totalReservedInDay = perTimeSlotRaw.Sum(x => x.ReservedCapacity);
            var remainingCapacityForDay = totalRestaurantCapacity - totalReservedInDay;

            // 6. Kết quả trả về
            var result = new DayCapacitySummaryDto
            {
                Date = date.Date,
                TotalRestaurantCapacity = totalRestaurantCapacity,
                TotalReservedGuestsInDay = totalReservedInDay,        // tổng chỗ đã xếp trong ngày
                RemainingCapacityForDay = remainingCapacityForDay,    // còn lại trong ngày
                TimeSlots = timeSlotDtos
            };

            return result;
        }

    }
}
