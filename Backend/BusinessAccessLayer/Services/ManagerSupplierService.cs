using AutoMapper;
using BusinessAccessLayer.DTOs.Inventory;
using BusinessAccessLayer.DTOs.Manager;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    public class ManagerSupplierService : IManagerSupplierService
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ManagerSupplierService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        public Task<bool> AddRecipe(SupplierDTO dto)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteSupplierByMenuItemId(int idSupplier)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<SupplierDTO>> GetManagerAllSupplier()
        {
            var supplier = await _unitOfWork.Supplier.GetAllAsync();
            return _mapper.Map<IEnumerable<SupplierDTO>>(supplier);
        }

        public Task<SupplierDTO> ManagerSupplierById(int id)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateSupplier(SupplierDTO updateSupplier)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> CreateSupplier(CreateSupplierDTO dto)
        {
            try
            {
                // Kiểm tra mã trùng
                var exists = await _unitOfWork.Supplier.CheckCodeExistsAsync(dto.CodeSupplier);
                if (exists)
                {
                    throw new InvalidOperationException("Mã nhà cung cấp đã tồn tại");
                }

                var supplier = _mapper.Map<Supplier>(dto);
                await _unitOfWork.Supplier.AddAsync(supplier);
                await _unitOfWork.Supplier.SaveChangesAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }

        //  IMPLEMENT UPDATE
        public async Task<bool> UpdateSupplier(int id, UpdateSupplierDTO dto)
        {
            try
            {
                var existingSupplier = await _unitOfWork.Supplier.GetByIdAsync(id);
                if (existingSupplier == null)
                {
                    throw new InvalidOperationException("Không tìm thấy nhà cung cấp");
                }

                // Cập nhật các trường (không bao gồm Code)
                existingSupplier.Name = dto.Name;
                existingSupplier.ContactInfo = dto.ContactInfo;
                existingSupplier.Phone = dto.Phone;
                existingSupplier.Email = dto.Email;
                existingSupplier.Address = dto.Address;

                await _unitOfWork.Supplier.UpdateAsync(existingSupplier);
                await _unitOfWork.Supplier.SaveChangesAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }

        //  IMPLEMENT CHECK CODE
        public async Task<bool> CheckCodeExists(string code)
        {
            return await _unitOfWork.Supplier.CheckCodeExistsAsync(code);
        }

    }
}
