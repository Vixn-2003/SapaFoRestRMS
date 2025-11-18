using BusinessAccessLayer.DTOs.Inventory;
using BusinessAccessLayer.Services;
using BusinessAccessLayer.Services.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PurchaseOrderController : ControllerBase
    {

        private readonly IPurchaseOrderService _purchaseOrderService;
        private readonly IInventoryIngredientService _inventoryIngredientService;
        private readonly IStockTransactionService _stockTransactionService;
        private readonly IUnitService _unitService;
        private readonly IWarehouseService _warehouseService;
        public PurchaseOrderController(IWarehouseService warehouseService,IPurchaseOrderService purchaseOrderService, IInventoryIngredientService inventoryIngredientService, IStockTransactionService stockTransactionService, IUnitService unitService)
        {
            _purchaseOrderService = purchaseOrderService;
            _inventoryIngredientService = inventoryIngredientService;
            _stockTransactionService = stockTransactionService;
            _unitService = unitService;
            _warehouseService = warehouseService;
        }

       [HttpGet]
       public async Task<ActionResult<IEnumerable<PurchaseOrderDTO>>> AllPurchaseOrder()
        {
            try
            {
                var purchaseOrder = await _purchaseOrderService.GetAll();

                if (!purchaseOrder.Any())
                    return NotFound("No purchase order found");

                return Ok(purchaseOrder);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("Detail/{id}")]
        public async Task<ActionResult<PurchaseOrderDTO>> PurchaseOrderById(string id )
        {
            try
            {
                var purchaseOrder = await _purchaseOrderService.GetPurchaseOrderById(id);

                if (purchaseOrder == null)
                    return NotFound("No purchase order found");

                return Ok(purchaseOrder);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("Confirm")]
        public async Task<IActionResult> Confirm([FromBody] ConfirmPurchaseOrderRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });

            try
            {
                //  XỬ LÝ TỪ CHỐI ĐƠN HÀNG
                if (request.Status == "Cancelled")
                {
                    var result = await _purchaseOrderService.ConfirmOrder(
                        request.PurchaseOrderId,
                        request.CheckId,
                        request.TimeConfirm,
                        request.Status
                    );

                    if (!result)
                        return BadRequest(new { success = false, message = "Không thể từ chối đơn hàng." });

                    return Ok(new
                    {
                        success = true,
                        message = "Đã từ chối đơn nhập hàng thành công!",
                        purchaseOrderId = request.PurchaseOrderId,
                        status = "Cancelled",
                        rejectReason = request.RejectReason
                    });
                }

                //  XỬ LÝ XÁC NHẬN ĐƠN HÀNG (Status = "Completed")
                var listIngredients = await _purchaseOrderService.GetPurchaseOrderById(request.PurchaseOrderId);

                if (listIngredients == null || listIngredients.PurchaseOrderDetails == null || !listIngredients.PurchaseOrderDetails.Any())
                {
                    return BadRequest(new { success = false, message = "Danh sách nguyên liệu trống hoặc không tồn tại." });
                }

                // Xử lý từng nguyên liệu
                foreach (var item in listIngredients.PurchaseOrderDetails)
                {
                    int idW = await _warehouseService.GetWarehouseByString(item.WarehouseName);
                    if (item.IngredientId == null)
                    {
                        int idU = await _unitService.getIdUnitByString(item.Unit);                       
                        //  NGUYÊN LIỆU MỚI - Thêm vào bảng Ingredient trước
                        IngredientDTO ingredients = new IngredientDTO
                        {
                            Name = item.IngredientName,
                            IngredientCode = item.IngredientCode,
                            UnitId = idU,
                            ReorderLevel = 10
                        };

                        int id = await _inventoryIngredientService.AddNewIngredient(ingredients);

                        if (id == 0)
                            return BadRequest(new { success = false, message = $"Không thể thêm nguyên liệu mới: {item.IngredientName}" });

                        item.IngredientId = id;

                        // Cập nhật IngredientId vào PurchaseOrderDetail
                        bool updateResult = await _purchaseOrderService.AddIdNewIngredient(item.PurchaseOrderDetailId, id);

                        if (!updateResult)
                            return BadRequest(new { success = false, message = $"Không thể cập nhật ID cho nguyên liệu: {item.IngredientName}" });

                        // Tạo Batch mới
                        InventoryBatchDTO batchIngredientDTO = new InventoryBatchDTO
                        {
                            IngredientId = id,
                            PurchaseOrderDetailId = item.PurchaseOrderDetailId,
                            ExpiryDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-5)),
                            WarehouseId = idW,
                            QuantityRemaining = item.Quantity
                        };

                        int idBatch = await _inventoryIngredientService.AddNewBatch(batchIngredientDTO);

                        if (idBatch == 0)
                            return BadRequest(new { success = false, message = $"Lỗi không thể thêm vào kho cho: {item.IngredientName}" });

                        // Tạo StockTransaction
                        StockTransactionDTO stockTransactionDTO = new StockTransactionDTO
                        {
                            IngredientId = id,
                            Quantity = item.Quantity,
                            BatchId = idBatch,
                            Note = $"Nhập hàng từ đơn {request.PurchaseOrderId}",
                            Type = "Import",
                            TransactionDate = DateTime.Now,
                        };

                        bool stockResponse = await _stockTransactionService.AddIdNewStock(stockTransactionDTO);

                        if (!stockResponse)
                            return BadRequest(new { success = false, message = $"Lỗi khi lưu lịch sử nhập cho: {item.IngredientName}" });
                    }
                    else
                    {
                        //  NGUYÊN LIỆU CÓ SẴN - Chỉ cần thêm Batch và StockTransaction
                        InventoryBatchDTO batchIngredientDTO = new InventoryBatchDTO
                        {
                            IngredientId = (int)item.IngredientId,
                            PurchaseOrderDetailId = item.PurchaseOrderDetailId,
                            ExpiryDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-5)),
                            WarehouseId = idW,
                            QuantityRemaining = item.Quantity
                        };

                        int idBatch = await _inventoryIngredientService.AddNewBatch(batchIngredientDTO);

                        if (idBatch == 0)
                            return BadRequest(new { success = false, message = $"Lỗi không thể thêm vào kho cho: {item.IngredientName}" });

                        StockTransactionDTO stockTransactionDTO = new StockTransactionDTO
                        {
                            IngredientId = (int)item.IngredientId,
                            Quantity = item.Quantity,
                            BatchId = idBatch,
                            Note = $"Nhập hàng từ đơn {request.PurchaseOrderId}",
                            Type = "Import",
                            TransactionDate = DateTime.Now,
                        };

                        bool stockResponse = await _stockTransactionService.AddIdNewStock(stockTransactionDTO);

                        if (!stockResponse)
                            return BadRequest(new { success = false, message = $"Lỗi khi lưu lịch sử nhập cho: {item.IngredientName}" });
                    }
                }

                // Cập nhật trạng thái đơn hàng thành "Completed"
                var confirmResult = await _purchaseOrderService.ConfirmOrder(
                    request.PurchaseOrderId,
                    request.CheckId,
                    request.TimeConfirm,
                    request.Status
                );

                if (!confirmResult)
                    return BadRequest(new { success = false, message = "Không thể xác nhận đơn hàng." });

                return Ok(new
                {
                    success = true,
                    message = "Xác nhận đơn nhập hàng thành công!",
                    purchaseOrderId = request.PurchaseOrderId,
                    status = "Completed",
                    totalItems = listIngredients.PurchaseOrderDetails.Count,
                    confirmedAt = DateTime.Now
                });
            }
            catch (Exception ex)
            {

                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi server: " + ex.Message,
                    detail = ex.StackTrace
                });
            }
        }
    }
}
