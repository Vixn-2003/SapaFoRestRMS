using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories
{
    public class ReservationDepositRepository : IReservationDepositRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public ReservationDepositRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        public async Task<ReservationDeposit> CreateAsync(ReservationDeposit deposit)
        {
            _context.ReservationDeposits.Add(deposit);
            await _context.SaveChangesAsync();
            return deposit;
        }

        public async Task<ReservationDeposit?> GetByIdAsync(int id)
        {
            return await _context.ReservationDeposits
                .Include(d => d.Reservation)
                .FirstOrDefaultAsync(d => d.DepositId == id);
        }
        public async Task<List<ReservationDeposit>> GetByReservationIdAsync(int reservationId)
        {
            return await _context.ReservationDeposits
                .Where(d => d.ReservationId == reservationId)
                .OrderByDescending(d => d.DepositDate)
                .ToListAsync();
        }

        public async Task DeleteAsync(int depositId)
        {
            var deposit = await _context.ReservationDeposits.FindAsync(depositId);
            if (deposit != null)
            {
                _context.ReservationDeposits.Remove(deposit);
                await _context.SaveChangesAsync();
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
