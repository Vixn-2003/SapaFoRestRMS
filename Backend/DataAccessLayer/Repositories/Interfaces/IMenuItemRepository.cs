using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IMenuItemRepository : IRepository<MenuItem>
    {
        Task<IEnumerable<(MenuItem MenuItem, int TotalQuantity)>> GetTopBestSellersAsync(int top = 10);
    }
}
