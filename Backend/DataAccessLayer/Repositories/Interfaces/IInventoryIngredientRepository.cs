using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IInventoryIngredientRepository : IRepository<Ingredient>
    {
        Task<(decimal totalImport, decimal totalExport, decimal totalFirst)> GetTotalImportExportBatches(int ingredientId, DateTime? startDate, DateTime? endDate);

        Task<IEnumerable<InventoryBatch>> getBatchById(int id);
    }
}
