using AutoMapper;
using BusinessAccessLayer.DTOs.Positions;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    public class PositionService : IPositionService
    {
        private readonly IPositionRepository _positionRepository;
        private readonly IMapper _mapper;

        public PositionService(IPositionRepository positionRepository, IMapper mapper)
        {
            _positionRepository = positionRepository;
            _mapper = mapper;
        }

        public async Task<System.Collections.Generic.IEnumerable<PositionDto>> GetAllAsync(CancellationToken ct = default)
        {
            var positions = await _positionRepository.GetAllAsync();
            return _mapper.Map<System.Collections.Generic.IEnumerable<PositionDto>>(positions);
        }

        public async Task<PositionDto?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var position = await _positionRepository.GetByIdAsync(id);
            if (position == null)
            {
                return null;
            }
            return _mapper.Map<PositionDto>(position);
        }

        public async Task<PositionListResponse> SearchAsync(PositionSearchRequest request, CancellationToken ct = default)
        {
            // Validate pagination
            var page = request.Page <= 0 ? 1 : request.Page;
            var pageSize = request.PageSize <= 0 ? 10 : (request.PageSize > 200 ? 200 : request.PageSize);

            var (items, totalCount) = await _positionRepository.SearchWithPaginationAsync(
                request.SearchTerm,
                request.Status,
                page,
                pageSize,
                ct);

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var positionDtos = _mapper.Map<System.Collections.Generic.List<PositionDto>>(items);

            return new PositionListResponse
            {
                Items = positionDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasPreviousPage = page > 1,
                HasNextPage = page < totalPages
            };
        }

        public async Task<PositionDto> CreateAsync(PositionCreateRequest request, CancellationToken ct = default)
        {
            // Business validation
            if (await _positionRepository.IsNameExistsAsync(request.PositionName, ct))
            {
                throw new InvalidOperationException("Position with the same name already exists");
            }

            // Business rule: Default status to 0 if invalid
            var status = (request.Status >= 0 && request.Status <= 2) ? request.Status : 0;

            var position = new Position
            {
                PositionName = request.PositionName,
                Description = request.Description,
                Status = status
            };

            await _positionRepository.AddAsync(position);
            await _positionRepository.SaveChangesAsync();

            return _mapper.Map<PositionDto>(position);
        }

        public async Task UpdateAsync(int id, PositionUpdateRequest request, CancellationToken ct = default)
        {
            var position = await _positionRepository.GetByIdAsync(id);
            if (position == null)
            {
                throw new InvalidOperationException("Position not found");
            }

            // Check if name changed and if new name already exists
            if (!string.Equals(position.PositionName, request.PositionName, StringComparison.OrdinalIgnoreCase))
            {
                if (await _positionRepository.IsNameExistsAsync(request.PositionName, id, ct))
                {
                    throw new InvalidOperationException("Position with the same name already exists");
                }
            }

            // Update properties
            position.PositionName = request.PositionName;
            position.Description = request.Description;
            position.Status = request.Status;

            await _positionRepository.UpdateAsync(position);
            await _positionRepository.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var position = await _positionRepository.GetByIdAsync(id);
            if (position == null)
            {
                throw new InvalidOperationException("Position not found");
            }

            await _positionRepository.DeleteAsync(id);
            await _positionRepository.SaveChangesAsync();
        }

        public async Task ChangeStatusAsync(int id, int status, CancellationToken ct = default)
        {
            if (status < 0 || status > 2)
            {
                throw new ArgumentException("Status must be between 0 and 2");
            }

            var position = await _positionRepository.GetByIdAsync(id);
            if (position == null)
            {
                throw new InvalidOperationException("Position not found");
            }

            position.Status = status;

            await _positionRepository.UpdateAsync(position);
            await _positionRepository.SaveChangesAsync();
        }
    }
}

