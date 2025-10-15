using AutoMapper;
using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    public class MarketingCampaignService : IMarketingCampaignService
    {
        private readonly IMarketingCampaignRepository _repository;
        private readonly IMapper _mapper;
        private readonly ICloudinaryService _cloudinaryService;

        public MarketingCampaignService(
             IMarketingCampaignRepository repository,
             IMapper mapper,
             ICloudinaryService cloudinaryService)
        {
            _repository = repository;
            _mapper = mapper;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<IEnumerable<MarketingCampaignDto>> GetAllAsync()
        {
            var campaigns = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<MarketingCampaignDto>>(campaigns);
        }

        public async Task<MarketingCampaignDto?> GetByIdAsync(int id)
        {
            var campaign = await _repository.GetByIdAsync(id);
            return campaign == null ? null : _mapper.Map<MarketingCampaignDto>(campaign);
        }

        public async Task<MarketingCampaignDto> CreateAsync(MarketingCampaignCreateDto dto, IFormFile? imageFile)
        {
            var campaign = _mapper.Map<MarketingCampaign>(dto);

            // Upload image to Cloudinary if provided
            if (imageFile != null)
            {
                campaign.ImageUrl = await _cloudinaryService.UploadImageAsync(imageFile, "campaigns");
            }

            var created = await _repository.AddAsync(campaign);
            return _mapper.Map<MarketingCampaignDto>(created);
        }

        public async Task<MarketingCampaignDto?> UpdateAsync(int id, MarketingCampaignUpdateDto dto, IFormFile? imageFile)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return null;

            // Map non-null properties from DTO
            _mapper.Map(dto, existing);

            // Upload new image if provided
            if (imageFile != null)
            {
                // Delete old image if exists
                if (!string.IsNullOrEmpty(existing.ImageUrl))
                {
                    await _cloudinaryService.DeleteImageAsync(existing.ImageUrl);
                }

                existing.ImageUrl = await _cloudinaryService.UploadImageAsync(imageFile, "campaigns");
            }

            await _repository.UpdateAsync(existing);
            return _mapper.Map<MarketingCampaignDto>(existing);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var exists = await _repository.ExistsAsync(id);
            if (!exists)
                return false;

            await _repository.DeleteAsync(id);
            return true;
        }

        public async Task<(IEnumerable<MarketingCampaignDto> data, int totalCount)> SearchFilterPaginateAsync(
            string? searchTerm,
            string? campaignType,
            string? status,
            DateOnly? startDate,
            DateOnly? endDate,
            int pageNumber,
            int pageSize)
        {
            var (campaigns, totalCount) = await _repository.SearchFilterPaginateAsync(
                searchTerm, campaignType, status, startDate, endDate, pageNumber, pageSize);

            var dtos = _mapper.Map<IEnumerable<MarketingCampaignDto>>(campaigns);
            return (dtos, totalCount);
        }

        public async Task<CampaignKpiDto> GetKpisAsync(DateOnly? startDate, DateOnly? endDate)
        {
            var totalCampaigns = await _repository.GetTotalCampaignsAsync(startDate, endDate);
            var totalBudget = await _repository.GetTotalBudgetSpentAsync(startDate, endDate);
            var totalRevenue = await _repository.GetTotalRevenueGeneratedAsync(startDate, endDate);
            var avgConversionRate = await _repository.GetAverageConversionRateAsync(startDate, endDate);
            var totalROI = await _repository.GetTotalROIAsync(startDate, endDate);

            return new CampaignKpiDto
            {
                TotalCampaigns = totalCampaigns,
                TotalBudgetSpent = totalBudget,
                TotalRevenueGenerated = totalRevenue,
                AvgConversionRate = avgConversionRate,
                TotalROI = totalROI
            };
        }

        public async Task<IEnumerable<CampaignPerformanceDto>> GetPerformanceChartDataAsync(
            DateOnly startDate, DateOnly endDate)
        {
            var data = await _repository.GetPerformanceDataAsync(startDate, endDate);

            return data.Select(d => new CampaignPerformanceDto
            {
                Month = d.Month,
                Revenue = d.Revenue,
                Reach = d.Reach,
                Budget = d.Budget
            });
        }

        public async Task<CampaignComparisonDto> GetYearOverYearComparisonAsync(
            DateOnly startDate, DateOnly endDate)
        {
            var currentData = await _repository.GetPerformanceDataAsync(startDate, endDate);
            var previousData = await _repository.GetPerformanceDataForPreviousYearAsync(startDate, endDate);

            var currentRevenue = currentData.Sum(d => d.Revenue);
            var currentReach = currentData.Sum(d => d.Reach);
            var currentBudget = currentData.Sum(d => d.Budget);

            var previousRevenue = previousData.Sum(d => d.Revenue);
            var previousReach = previousData.Sum(d => d.Reach);
            var previousBudget = previousData.Sum(d => d.Budget);

            var revenueGrowth = previousRevenue > 0
                ? ((currentRevenue - previousRevenue) / previousRevenue) * 100
                : 0;

            var reachGrowth = previousReach > 0
                ? (((decimal)currentReach - previousReach) / previousReach) * 100
                : 0;

            var currentROI = currentBudget > 0
                ? ((currentRevenue - currentBudget) / currentBudget) * 100
                : 0;

            var previousROI = previousBudget > 0
                ? ((previousRevenue - previousBudget) / previousBudget) * 100
                : 0;

            return new CampaignComparisonDto
            {
                CurrentRevenue = currentRevenue,
                PreviousYearRevenue = previousRevenue,
                RevenueGrowthPercentage = revenueGrowth,
                CurrentReach = currentReach,
                PreviousYearReach = previousReach,
                ReachGrowthPercentage = reachGrowth,
                CurrentROI = currentROI,
                PreviousYearROI = previousROI
            };
        }

        public async Task<CampaignTargetKpiDto> GetTargetKpiAsync(
            DateOnly startDate,
            DateOnly endDate,
            decimal targetRevenue,
            int targetReach)
        {
            var (currentRevenue, currentReach) = await _repository.GetCurrentPeriodMetricsAsync(startDate, endDate);

            return new CampaignTargetKpiDto
            {
                TargetRevenue = targetRevenue,
                TargetReach = targetReach,
                CurrentRevenue = currentRevenue,
                CurrentReach = currentReach
            };
        }

        public async Task<IEnumerable<CampaignDistributionDto>> GetCampaignDistributionAsync()
        {
            var data = await _repository.GetCampaignDistributionAsync();

            return data.Select(d => new CampaignDistributionDto
            {
                Type = d.Type,
                Count = d.Count
            });
        }
    }
}