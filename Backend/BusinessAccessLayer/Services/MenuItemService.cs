using AutoMapper;
using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    public class MenuItemService : IMenuItemService
    {
        private readonly IMenuItemRepository _menuItemRepository;
        private readonly IMapper _mapper;

        public MenuItemService(IMenuItemRepository menuItemRepository, IMapper mapper)
        {
            _menuItemRepository = menuItemRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<BestSellerDto>> GetTopBestSellersAsync()
        {
            var data = await _menuItemRepository.GetTopBestSellersAsync();

            return _mapper.Map<IEnumerable<BestSellerDto>>(data);



        }
    }
}
