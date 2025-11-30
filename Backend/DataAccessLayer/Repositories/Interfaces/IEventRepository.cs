using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IEventRepository
    {
        Task<List<Event>> GetAllAsync();
        Task<Event?> GetByIdAsync(int id);
        Task AddAsync(Event ev);
        Task UpdateAsync(Event ev);
        Task DeleteAsync(Event ev);
    }
}
