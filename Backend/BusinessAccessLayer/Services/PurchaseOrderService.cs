using AutoMapper;
using BusinessAccessLayer.DTOs.Inventory;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    public class PurchaseOrderService : IPurchaseOrderService
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public PurchaseOrderService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<bool> AddIdNewIngredient(int idDetailOrder, int idIngredient)
        {
            var purchaseOrder = await _unitOfWork.PurchaseOrder.AddIdNewIngredient(idDetailOrder, idIngredient);
            return purchaseOrder;
        }

        public async Task<bool> ConfirmOrder(string PurchaseOrderId, int idChecker, DateTime time, string status)
        {
            var result = await _unitOfWork.PurchaseOrder.ConfirmOrder(PurchaseOrderId,idChecker,time, status);
            return result;
        }

        public async Task<bool> CreateImportOrderAsync(ImportOrder importOrder, List<ImportDetail> importDetails)
        {
            // Map ImportOrder → PurchaseOrder
            var purchaseOrder = new PurchaseOrder
            {
                PurchaseOrderId = importOrder.ImportCode,
                SupplierId = importOrder.SupplierId,
                IdCreator = importOrder.CreatorId,
               // IdConfirm = importOrder.CheckId,
                UrlImg = importOrder.ProofImagePath,
                OrderDate = importOrder.ImportDate,
                Status = "Processing"
            };

            // Map ImportDetail → PurchaseOrderDetail
            var details = importDetails.Select(d => new PurchaseOrderDetail
            {
                IngredientId = d.IngredientId,
                IngredientCode = d.IngredientCode,
                IngredientName = d.IngredientName,
                Unit = d.Unit,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                Subtotal = d.TotalPrice,
                WarehouseName = d.WarehouseName
            }).ToList();

            return await _unitOfWork.PurchaseOrder.CreatePurchaseOrderAsync(purchaseOrder, details);
        }

        public async Task<IEnumerable<PurchaseOrderDTO>> GetAll()
        {
            var purchaseOrder = await _unitOfWork.PurchaseOrder.GetAllAsync();
            return _mapper.Map<IEnumerable<PurchaseOrderDTO>>(purchaseOrder);
        }

        public async Task<PurchaseOrderDTO> GetPurchaseOrderById(string id)
        {
            var purchaseOrder = await _unitOfWork.PurchaseOrder.GetByIdPurchase(id);
            return _mapper.Map<PurchaseOrderDTO>(purchaseOrder);
        }
    }
}
