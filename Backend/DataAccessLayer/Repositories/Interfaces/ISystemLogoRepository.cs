using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface ISystemLogoRepository : IRepository<SystemLogo>
    {
        IEnumerable<SystemLogo> GetActiveLogos();
        Task<SystemLogo?> GetByIdAsync(int id); // lấy theo id
        Task AddAsync(SystemLogo logo);
        void Update(SystemLogo logo);
        void Delete(SystemLogo logo);
    }
}
