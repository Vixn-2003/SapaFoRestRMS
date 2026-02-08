using AutoMapper;
using BusinessAccessLayer.DTOs.Positions;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services;

/// <summary>
/// Service xử lý business logic cho SalaryChangeRequest
/// </summary>
public class SalaryChangeRequestService : ISalaryChangeRequestService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISalaryChangeRequestRepository _salaryChangeRequestRepository;
    private readonly IMapper _mapper;

    public SalaryChangeRequestService(
        IUnitOfWork unitOfWork,
        ISalaryChangeRequestRepository salaryChangeRequestRepository,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _salaryChangeRequestRepository = salaryChangeRequestRepository;
        _mapper = mapper;
    }

    public async Task<SalaryChangeRequestDto> CreateRequestAsync(CreateSalaryChangeRequestDto request, int requestedByUserId, CancellationToken ct = default)
    {
        // Kiểm tra Position tồn tại
        var position = await _unitOfWork.Positions.GetByIdAsync(request.PositionId);
        if (position == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy Position với ID: {request.PositionId}");
        }

        // Business validation: ProposedBaseSalary phải lớn hơn 0
        if (request.ProposedBaseSalary <= 0)
        {
            throw new ArgumentException("Lương đề xuất phải lớn hơn 0");
        }

        // Business validation: Kiểm tra xem có yêu cầu Pending nào cho Position này không
        var existingPendingRequest = (await _salaryChangeRequestRepository.GetByPositionIdAsync(request.PositionId))
            .FirstOrDefault(r => r.Status == "Pending");
        
        if (existingPendingRequest != null)
        {
            throw new InvalidOperationException($"Đã có yêu cầu thay đổi lương đang chờ phê duyệt cho Position này. Vui lòng đợi Owner xử lý yêu cầu ID: {existingPendingRequest.RequestId}");
        }

        // Tạo yêu cầu
        var salaryChangeRequest = new SalaryChangeRequest
        {
            PositionId = request.PositionId,
            CurrentBaseSalary = position.BaseSalary, // Lưu lương hiện tại để so sánh
            ProposedBaseSalary = request.ProposedBaseSalary,
            Reason = request.Reason,
            Status = "Pending",
            RequestedBy = requestedByUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _salaryChangeRequestRepository.AddAsync(salaryChangeRequest);
        await _unitOfWork.SaveChangesAsync();

        // Load lại với details
        var savedRequest = await _salaryChangeRequestRepository.GetByIdWithDetailsAsync(salaryChangeRequest.RequestId);
        return _mapper.Map<SalaryChangeRequestDto>(savedRequest);
    }

    public async Task<IEnumerable<SalaryChangeRequestDto>> GetPendingRequestsAsync(CancellationToken ct = default)
    {
        var requests = await _salaryChangeRequestRepository.GetPendingRequestsAsync();
        return _mapper.Map<IEnumerable<SalaryChangeRequestDto>>(requests);
    }

    public async Task<IEnumerable<SalaryChangeRequestDto>> GetAllRequestsAsync(string? status = null, CancellationToken ct = default)
    {
        IEnumerable<SalaryChangeRequest> requests;

        if (string.IsNullOrWhiteSpace(status))
        {
            requests = await _salaryChangeRequestRepository.GetAllAsync();
        }
        else
        {
            requests = await _salaryChangeRequestRepository.GetByStatusAsync(status);
        }

        return _mapper.Map<IEnumerable<SalaryChangeRequestDto>>(requests);
    }

    public async Task<SalaryChangeRequestDto> ReviewRequestAsync(ReviewSalaryChangeRequestDto request, int approvedByUserId, CancellationToken ct = default)
    {
        var salaryRequest = await _salaryChangeRequestRepository.GetByIdWithDetailsAsync(request.RequestId);
        
        if (salaryRequest == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy yêu cầu với ID: {request.RequestId}");
        }

        if (salaryRequest.Status != "Pending")
        {
            throw new InvalidOperationException($"Yêu cầu này đã được xử lý. Trạng thái hiện tại: {salaryRequest.Status}");
        }

        // Cập nhật trạng thái
        if (request.Action.Equals("Approve", StringComparison.OrdinalIgnoreCase))
        {
            salaryRequest.Status = "Approved";
            
            // Cập nhật BaseSalary của Position (chỉ khi Owner approve)
            var position = await _unitOfWork.Positions.GetByIdAsync(salaryRequest.PositionId);
            if (position != null)
            {
                // Kiểm tra xem BaseSalary hiện tại có thay đổi không (có thể có yêu cầu khác đã được approve)
                if (position.BaseSalary != salaryRequest.CurrentBaseSalary)
                {
                    // BaseSalary đã thay đổi, cần cảnh báo Owner
                    // Nhưng vẫn cập nhật theo yêu cầu đã được approve
                    // Có thể log hoặc thông báo cho Owner biết
                }
                
                position.BaseSalary = salaryRequest.ProposedBaseSalary;
                await _unitOfWork.Positions.UpdateAsync(position);
            }
        }
        else if (request.Action.Equals("Reject", StringComparison.OrdinalIgnoreCase))
        {
            salaryRequest.Status = "Rejected";
            // BaseSalary không thay đổi khi reject
        }
        else
        {
            throw new ArgumentException("Action phải là 'Approve' hoặc 'Reject'");
        }

        salaryRequest.ApprovedBy = approvedByUserId;
        salaryRequest.OwnerNotes = request.OwnerNotes;
        salaryRequest.ReviewedAt = DateTime.UtcNow;

        await _salaryChangeRequestRepository.UpdateAsync(salaryRequest);
        await _unitOfWork.SaveChangesAsync();

        // Load lại với details
        var updatedRequest = await _salaryChangeRequestRepository.GetByIdWithDetailsAsync(request.RequestId);
        return _mapper.Map<SalaryChangeRequestDto>(updatedRequest);
    }

    public async Task<IEnumerable<SalaryChangeRequestDto>> GetMyRequestsAsync(int userId, CancellationToken ct = default)
    {
        var requests = await _salaryChangeRequestRepository.GetByRequestedByAsync(userId);
        return _mapper.Map<IEnumerable<SalaryChangeRequestDto>>(requests);
    }

    public async Task<SalaryChangeRequestStatisticsDto> GetStatisticsAsync(CancellationToken ct = default)
    {
        var allRequests = await _salaryChangeRequestRepository.GetAllAsync();
        var requestsList = allRequests.ToList();

        var statistics = new SalaryChangeRequestStatisticsDto
        {
            TotalRequests = requestsList.Count,
            PendingRequests = requestsList.Count(r => r.Status == "Pending"),
            ApprovedRequests = requestsList.Count(r => r.Status == "Approved"),
            RejectedRequests = requestsList.Count(r => r.Status == "Rejected")
        };

        // Tính tổng và trung bình mức tăng lương đề xuất
        var approvedRequests = requestsList.Where(r => r.Status == "Approved").ToList();
        if (approvedRequests.Any())
        {
            statistics.TotalProposedIncrease = approvedRequests.Sum(r => r.ProposedBaseSalary - r.CurrentBaseSalary);
            statistics.AverageProposedIncrease = statistics.TotalProposedIncrease / approvedRequests.Count;
        }

        return statistics;
    }

    public async Task<SalaryChangeRequestDto?> GetByIdAsync(int requestId, CancellationToken ct = default)
    {
        var request = await _salaryChangeRequestRepository.GetByIdWithDetailsAsync(requestId);
        if (request == null)
        {
            return null;
        }

        return _mapper.Map<SalaryChangeRequestDto>(request);
    }
}

