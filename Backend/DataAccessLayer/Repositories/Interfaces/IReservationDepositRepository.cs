using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IReservationDepositRepository
    {
        Task<ReservationDeposit> CreateAsync(ReservationDeposit deposit);
        Task<ReservationDeposit?> GetByIdAsync(int id);
        Task<List<ReservationDeposit>> GetByReservationIdAsync(int reservationId);
        Task DeleteAsync(int depositId);

        Task SaveChangesAsync();
    }
}
