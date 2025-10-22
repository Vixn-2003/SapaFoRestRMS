using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    public class OrderTableService : IOrderTableService
    {
        private readonly IOrderTableRepository _orderTableRepository;

        public OrderTableService(IOrderTableRepository orderTableRepository)
        {
            _orderTableRepository = orderTableRepository;
        }

        public async Task<IEnumerable<TableOrderDto>> GetTablesByReservationStatusAsync(string status)
        {
            var reservations = await _orderTableRepository.GetReservationsByStatusAsync(status);

            return reservations
                .SelectMany(r => r.ReservationTables.Select(rt => new TableOrderDto
                {
                    TableId = rt.Table.TableId,
                    TableNumber = rt.Table.TableNumber,
                    Capacity = rt.Table.Capacity,
                    Status = rt.Table.Status,
                    AreaName = rt.Table.Area?.AreaName,
                    Floor = rt.Table.Area?.Floor,
                    NumberGuest = r.NumberOfGuests //  lấy từ Reservation
                }))
                .DistinctBy(t => t.TableId) // tránh trùng bàn nếu có nhiều ReservationTable
                .ToList();
        }





        public class TableOrderDto
        {
            public int TableId { get; set; }
            public string TableNumber { get; set; }
            public int Capacity { get; set; }
            public string? Status { get; set; }
            public string? AreaName { get; set; }

            public int? Floor {  get; set; }

            public int NumberGuest { get; set; }
        }
       

    }
}

