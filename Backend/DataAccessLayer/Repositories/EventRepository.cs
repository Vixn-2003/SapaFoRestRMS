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
    public class EventRepository : IEventRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public EventRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        public async Task<List<Event>> GetAllAsync()
        {
            return await _context.Events
                .OrderByDescending(e => e.StartDate)
                .ToListAsync();
        }
    }
}
