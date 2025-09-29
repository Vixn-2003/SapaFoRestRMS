using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync(); // Gets all data.
        Task<T> GetByIdAsync(int id);      // Gets data by ID.
        Task CreateAsync(T entity);        // Create a new resource.
        Task UpdateAsync(T entity);        // Updates existing data.
        Task DeleteByIdAsync(int id);      // Deletes data by ID.
    }
}
