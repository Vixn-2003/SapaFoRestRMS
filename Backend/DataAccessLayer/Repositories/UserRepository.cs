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
    public class UserRepository : IUserRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public UserRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByPhoneAsync(string phone)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Phone == phone);
        }

        public async Task<User> CreateAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }
    }
}
