using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByPhoneAsync(string phone);
        Task<User> CreateAsync(User user);
    }

}
