using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IOrderTableRepository : IRepository<Reservation>
    {
        /// <summary>
        /// Lấy danh sách Reservation theo trạng thái (ví dụ: "Guest Seated", "Completed").
        /// </summary>
        /// <param name="status">Trạng thái đặt bàn.</param>
        Task<IEnumerable<Reservation>> GetReservationsByStatusAsync(string status);
    }

}
