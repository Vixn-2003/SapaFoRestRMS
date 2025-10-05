using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace DataAccessLayer.Repositories
{
    internal class UserRepository : Repository<User>, IUserRepository
    {

        private readonly SapaFoRestRmsContext _context;


        public UserRepository(SapaFoRestRmsContext context) : base(context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.UserId == id && u.IsDeleted == false);
        }

        public async Task<bool> changePassword(int id, string newPassword)
        {
            var user = await GetByIdAsync(id);
            if (user != null)
            {
                user.PasswordHash = newPassword;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.IsDeleted == false);
        }

        public async Task<bool> IsEmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email && u.IsDeleted == false);
        }

      

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users.Where(u => u.IsDeleted == false).ToListAsync();
        }

        public async Task AddAsync(User entity)

        {
           
            await _context.Users.AddAsync(entity);

        }

        public async Task UpdateAsync(User entity)

        {
            _context.Users.Update(entity);
        }

        public async Task DeleteAsync(int id)
        {
            var user = await GetByIdAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);

            }

        }

        public Task SaveChangesAsync()
        {
            throw new NotImplementedException();
        }
    }
}
