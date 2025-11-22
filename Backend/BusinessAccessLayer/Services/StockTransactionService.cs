using AutoMapper;
using BusinessAccessLayer.DTOs.Inventory;
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
    public class StockTransactionService : IStockTransactionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public StockTransactionService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        public async Task<bool> AddIdNewStock(StockTransactionDTO stockTransaction)
        {
            var stock = _mapper.Map<StockTransaction>(stockTransaction);
            var purchaseOrder = await _unitOfWork.StockTransaction.AddNewStockTransaction(stock);
            return purchaseOrder;
        }

        public async Task<IEnumerable<StockTransactionDTO>> GetExportTransactionsAsync()
        {
            var transactions = await _unitOfWork.StockTransaction.GetExportTransactionsAsync();
            var dtos = transactions.Select(t => new StockTransactionDTO
            {
                TransactionId = t.TransactionId,
                IngredientId = t.IngredientId,
                IngredientName = t.Ingredient?.Name,
                Type = t.Type,
                Quantity = t.Quantity,
                TransactionDate = t.TransactionDate,
                Note = t.Note,
                BatchId = t.BatchId,
                BatchName = t.BatchId.HasValue ? $"Lô {t.BatchId}" : null
            }).ToList();
            return dtos;
        }
    }
}
