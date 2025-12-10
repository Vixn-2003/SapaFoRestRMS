
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.DTOs;
using DataAccessLayer.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    public class CapacityStatisticsService : ICapacityStatisticsService
    {
        private readonly ICapacityStatisticsRepository _repository;

        public CapacityStatisticsService(ICapacityStatisticsRepository repository)
        {
            _repository = repository;
        }

        public async Task<DayCapacitySummaryDto> GetDayCapacityAsync(DateTime? date)
        {
            var targetDate = date?.Date ?? DateTime.Today;
            return await _repository.GetDayCapacityAsync(targetDate);
        }
    }
}
