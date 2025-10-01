using BusinessAccessLayer.DTOs;
using DataAccessLayer.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public class ComboService : IComboService
    {
        private readonly IComboRepository _comboRepository;

        public ComboService(IComboRepository comboRepository)
        {
            _comboRepository = comboRepository;
        }

        public async Task<List<ComboDto>> GetAllCombosAsync()
        {
            var combos = await _comboRepository.GetAllAsync();
            return combos.Select(c => new ComboDto
            {
                Name = c.Name,
                ImageUrl = c.ImageUrl,
                Description = c.Description,
                Price = c.Price
            }).ToList();
        }
    }
}
