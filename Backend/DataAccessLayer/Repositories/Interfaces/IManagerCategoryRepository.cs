
using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IManagerCategoryRepository : IRepository<MenuCategory>
    {
        Task<IEnumerable<MenuCategory>> GetManagerAllCategory();
        Task<List<string>> GetCategoryNamesAsync();
    }
}
