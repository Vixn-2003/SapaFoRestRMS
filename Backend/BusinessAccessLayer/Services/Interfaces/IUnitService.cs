using BusinessAccessLayer.DTOs.Inventory;
using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IUnitService
    {
        Task<IEnumerable<UnitDTO>> GetAllUnits();
        Task<int> getIdUnitByString (string unitName);
    }
}
