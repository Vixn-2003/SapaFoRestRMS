using AutoMapper;
using BusinessAccessLayer.DTOs.Inventory;
using BusinessAccessLayer.DTOs.Manager;
using BusinessAccessLayer.Services.Interfaces;
using CloudinaryDotNet;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    public class InventoryIngredientService : IInventoryIngredientService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public InventoryIngredientService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<InventoryIngredientDTO>> GetAllIngredient()
        {
            var ingredients = await _unitOfWork.InventoryIngredient.GetAllAsync();
                return _mapper.Map<IEnumerable<InventoryIngredientDTO>>(ingredients);
        }

        public Task<(decimal TImport, decimal TExport, decimal totalFirst)> GetImportExportBatchesId(int id, DateTime? StartDate , DateTime? EndDate)
        {
            var totalExIm = _unitOfWork.InventoryIngredient.GetTotalImportExportBatches(id, StartDate, EndDate);
            return totalExIm;
        }

        public async Task<IEnumerable<BatchIngredientDTO>> GetBatchesAsync(int ingredientId)
        {
            var batches = await _unitOfWork.InventoryIngredient.getBatchById(ingredientId);
            return _mapper.Map<IEnumerable<BatchIngredientDTO>>(batches);
        }

        public async Task<bool> UpdateBatchWarehouse(int idBatch, int idWarehouse)
        {
            var result = await _unitOfWork.InventoryIngredient.UpdateBatchWarehouse(idBatch,idWarehouse);
            return result;
        }

        public async Task<IEnumerable<InventoryIngredientDTO>> GetAllIngredientSearch(string search)
        {
            var ingredients = await _unitOfWork.InventoryIngredient.GetAllIngredientSearch(search);
            return _mapper.Map<IEnumerable<InventoryIngredientDTO>>(ingredients);
        }

        public async Task<int> AddNewIngredient(IngredientDTO ingredient)
        {
            var ingre = _mapper.Map<Ingredient>(ingredient);
            var result = await _unitOfWork.InventoryIngredient.AddNewIngredient(ingre);
            return result;
        }

        public async Task<int> AddNewBatch(InventoryBatchDTO batchIngredientDTO)
        {
            var batch = _mapper.Map<InventoryBatch>(batchIngredientDTO);
            var result = await _unitOfWork.InventoryIngredient.AddNewBatch(batch);
            return result;
        }

        public async Task<InventoryIngredientDTO> GetIngredientById(int id)
        {
            var ingredients = await _unitOfWork.InventoryIngredient.GetIngredientById(id);
            return _mapper.Map<InventoryIngredientDTO>(ingredients);
        }

        public async Task<(bool success, string message)> UpdateIngredient(int idIngredient, string nameIngredient, string unit)
        {
            var ingredients = await _unitOfWork.InventoryIngredient.UpdateInforIngredient(idIngredient, nameIngredient, unit);
            return ingredients;
        }
    }
}
