using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IManagerMenuRepository : IRepository<MenuItem>
    {

        Task<IEnumerable<MenuItem>> GetManagerAllMenus();     
        Task<MenuItem> ManagerMenuByIds(int id);

}
}
