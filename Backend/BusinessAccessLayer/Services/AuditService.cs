using AutoMapper;
using BusinessAccessLayer.DTOs.Inventory;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    public class AuditService : IAuditService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public AuditService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<string?> CheckExitsAuditStatus(int batchId)
        {
            var result = await _unitOfWork.AuditRepository.CheckExitsAuditStatusRe(batchId);
            return result;
        }

        public async Task<bool> ConfirmAuditAsync(string id, AuditInventoryResponseDTO request)
        {
            try
            {
                var batchId = await _unitOfWork.AuditRepository.GetBatchIdByIdReAsync(id);
                if (batchId == 0)
                {
                    Console.WriteLine(" Không tìm thấy BatchId từ AuditId");
                    return false;
                }


                var batch = await _unitOfWork.InventoryIngredient.getBatchByBatchId(batchId);
                if (batch == null)
                {
                    Console.WriteLine(" Không tìm thấy InventoryBatch");
                    return false;
                }

                var auditEntity = _mapper.Map<AuditInventory>(request);


                    var confirmResult = await _unitOfWork.AuditRepository.ConfirmAuditReAsync(id, auditEntity);
                    if (!confirmResult)
                    {
                        Console.WriteLine(" Không thể xác nhận audit để lưu vào DB");
                        return false;
                    }

                    if (request.IsAddition)
                    {
                        request.NewQuantity = request.OriginalQuantity + request.AdjustmentQuantity;
                    }
                    else
                    {
                        request.NewQuantity = request.OriginalQuantity - request.AdjustmentQuantity;
                    }

                    batch.QuantityRemaining = request.NewQuantity;
                     batch.ExpiryDate = request.ExpiryDate;


                    var updateBatchResult = await _unitOfWork.InventoryIngredient.UpdateBatchByBatch(batch);
                    if (!updateBatchResult)
                    {
                        Console.WriteLine(" Không thể cập nhật tồn kho sau khi audit");
                        return false;
                    }

                    return true;

            }
            catch (Exception ex)
            {
                Console.WriteLine($" Lỗi ConfirmAuditAsync: {ex.Message}");
                return false;
            }
        }


        public async Task<int> CountAuditAsync(string count)
        {
            var result = await _unitOfWork.AuditRepository.CountAsync(count);
            return result;
        }

        public async Task<bool> CreateAuditAsync(AuditInventory auditRecord)
        {
            var result = await _unitOfWork.AuditRepository.AddAsync(auditRecord);
            return result;
        }

        public async Task<IEnumerable<AuditInventoryResponseDTO>> GetAllAuditsAsync()
        {
            var ingredients = await _unitOfWork.AuditRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<AuditInventoryResponseDTO>>(ingredients);
        }

        public async Task<AuditInventoryResponseDTO> GetAuditByIdAsync(string id)
        {
            var ingredients = await _unitOfWork.AuditRepository.GetAuditByIdReAsync(id);
            return _mapper.Map<AuditInventoryResponseDTO>(ingredients);
        }
    }
}
