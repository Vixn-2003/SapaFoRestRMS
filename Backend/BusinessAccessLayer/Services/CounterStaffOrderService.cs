using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using BusinessAccessLayer.DTOs.CounterStaff;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;

namespace BusinessAccessLayer.Services
{
    /// <summary>
    /// Service implementation cho Counter Staff Order Management - UC123
    /// </summary>
    public class CounterStaffOrderService : ICounterStaffOrderService
    {
        private readonly ICounterStaffOrderRepository _orderRepository;
        private readonly IMapper _mapper;

        public CounterStaffOrderService(
            ICounterStaffOrderRepository orderRepository,
            IMapper mapper)
        {
            _orderRepository = orderRepository;
            _mapper = mapper;
        }

        public async Task<List<OrderListItemDto>> GetAllOrdersAsync(OrderListFilterDto filter, CancellationToken ct = default)
        {
            var orders = await _orderRepository.GetAllOrdersAsync(filter.Status, filter.Date);

            var orderDtos = _mapper.Map<List<OrderListItemDto>>(orders);

            // Additional filtering by search keyword if needed
            if (!string.IsNullOrWhiteSpace(filter.SearchKeyword))
            {
                orderDtos = orderDtos
                    .Where(o =>
                        o.OrderCode.Contains(filter.SearchKeyword, StringComparison.OrdinalIgnoreCase) ||
                        o.TableNumber.Contains(filter.SearchKeyword, StringComparison.OrdinalIgnoreCase) ||
                        o.CustomerName.Contains(filter.SearchKeyword, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Additional filtering by table number if needed
            if (!string.IsNullOrWhiteSpace(filter.TableNumber))
            {
                orderDtos = orderDtos
                    .Where(o => o.TableNumber.Contains(filter.TableNumber, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return orderDtos;
        }

        public async Task<OrderListItemDto?> GetOrderSummaryAsync(int orderId, CancellationToken ct = default)
        {
            var order = await _orderRepository.GetOrderSummaryAsync(orderId);
            if (order == null)
            {
                return null;
            }

            return _mapper.Map<OrderListItemDto>(order);
        }
    }
}

