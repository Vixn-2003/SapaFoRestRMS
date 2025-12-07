using BusinessAccessLayer.DTOs.Filter;
using BusinessAccessLayer.DTOs.Inventory;
using BusinessAccessLayer.DTOs.Manager;
using BusinessAccessLayer.Services.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;

namespace SapaFoRestRMSAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryIngredientController : Controller
    {

        private readonly IInventoryIngredientService _inventoryIngredientService;
        private readonly IWarehouseService _warehouseService;
        private readonly IStockTransactionService _stockTransactionService;
        private readonly IAuditService _auditService;
        private readonly ICloudinaryService _cloudinaryService;

        public InventoryIngredientController(IInventoryIngredientService inventoryIngredientService, IWarehouseService warehouseService, IAuditService auditService, ICloudinaryService cloudinaryService, IStockTransactionService stockTransactionService)
        {
            _inventoryIngredientService = inventoryIngredientService;
            _warehouseService = warehouseService;
            _stockTransactionService = stockTransactionService;
            _auditService = auditService;
            _cloudinaryService = cloudinaryService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryIngredientDTO>>> DisplayIngredents()
        {
            try
            {
                var ingredients = await _inventoryIngredientService.GetAllIngredient();

                foreach (var ingredient in ingredients)
                {
                    decimal TE = 0;
                    decimal TI = 0;
                    decimal TFirst = 0;
                    foreach (var b in ingredient.Batches)
                    {
                        var totalIE = await _inventoryIngredientService.GetImportExportBatchesId(
                            b.BatchId, DateTime.Now.AddDays(-7), DateTime.Now
                        );
                        TE += totalIE.TExport;
                        TI += totalIE.TImport;
                        TFirst = totalIE.totalFirst;

                    }
                    ingredient.TotalImport = TI;
                    ingredient.TotalExport = TE;
                    ingredient.OriginalQuantity = TFirst;
                }

                if (!ingredients.Any())
                    return NotFound("No ingredient found");

                return Ok(ingredients);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("filter")]
        public async Task<ActionResult<IEnumerable<InventoryIngredientDTO>>> FilterIngredents([FromBody] IngredientFilterRequest? request)
        {
            try
            {
                var fromDate = request?.FromDate ?? DateTime.Now.AddDays(-7);
                var toDate = request?.ToDate ?? DateTime.Now;
                var search = request?.SearchIngredent;
                IEnumerable<InventoryIngredientDTO> ingredients;
                if (string.IsNullOrEmpty(search))
                {
                    ingredients = await _inventoryIngredientService.GetAllIngredient();
                }
                else
                {
                    ingredients = await _inventoryIngredientService.GetAllIngredientSearch(search);
                }


                foreach (var ingredient in ingredients)
                {
                    decimal TE = 0;
                    decimal TI = 0;
                    decimal TFirst = 0;
                    foreach (var b in ingredient.Batches)
                    {
                        var totalIE = await _inventoryIngredientService.GetImportExportBatchesId(
                            b.BatchId, fromDate, toDate
                        );
                        TE += totalIE.TExport;
                        TI += totalIE.TImport;
                        TFirst = totalIE.totalFirst;

                    }
                    ingredient.TotalImport = TI;
                    ingredient.TotalExport = TE;
                    ingredient.OriginalQuantity = TFirst;
                }

                if (!ingredients.Any())
                    return NotFound("No ingredient found");

                return Ok(ingredients);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpGet("BatchIngredient/{id}")]
        public async Task<ActionResult<List<BatchIngredientDTO>>> GetBatchIngredient(int id)
        {
            try
            {
                var batches = await _inventoryIngredientService.GetBatchesAsync(id);

                var result = batches.ToList();

                if (result == null || result.Count == 0)
                {
                    return Ok(new List<BatchIngredientDTO>());
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error in GetBatchIngredient: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                return StatusCode(500, new
                {
                    message = "Có lỗi xảy ra khi lấy dữ liệu",
                    error = ex.Message
                });
            }
        }

        [HttpPut("UpdateBatchWarehouse")]
        public async Task<IActionResult> UpdateBatchWarehouse([FromBody] UpdateBatchWarehouseRequest request)
        {
            try
            {
                // _logger.LogInformation($"Nhận request cập nhật kho: BatchId={request.BatchId}, WarehouseId={request.WarehouseId}");

                // Validate request
                if (request.BatchId <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "BatchId không hợp lệ"
                    });
                }

                if (request.WarehouseId <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "WarehouseId không hợp lệ"
                    });
                }

                // Kiểm tra warehouse có tồn tại và active không
                var warehouseExists = await _warehouseService.GetWarehouseById(request.WarehouseId);
                if (warehouseExists == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Không tìm thấy kho #{request.WarehouseId}"
                    });
                }

                // Cập nhật warehouse cho batch
                var result = await _inventoryIngredientService.UpdateBatchWarehouse(request.BatchId, request.WarehouseId, request.IsActive);

                if (result)
                {
                    // _logger.LogInformation($"Cập nhật kho thành công: BatchId={request.BatchId}, WarehouseId={request.WarehouseId}");

                    return Ok(new
                    {
                        success = true,
                        message = "Cập nhật kho thành công",
                        data = new
                        {
                            batchId = request.BatchId,
                            warehouseId = request.WarehouseId
                        }
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Không thể cập nhật kho"
                    });
                }
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, $"Lỗi khi cập nhật kho cho batch {request.BatchId}");

                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi cập nhật kho",
                    error = ex.Message
                });
            }
        }

        public class UpdateBatchWarehouseRequest
        {
            public int BatchId { get; set; }
            public int WarehouseId { get; set; }

            public bool IsActive { get; set; }
        }

        [HttpPut("UpdateIngredient")]
        public async Task<IActionResult> UpdateIngredient([FromBody] UpdateIngredientRequest request)
        {
            try
            {
                // Validate request
                if (request.IngredientId <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "IngredientId không hợp lệ"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Tên nguyên liệu không được để trống"
                    });
                }

                // Gọi service để cập nhật
                var (success, message) = await _inventoryIngredientService.UpdateIngredient(
                    request.IngredientId,
                    request.Name.Trim(),
                    request.UnitId
                );

                if (success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = message,
                        data = new
                        {
                            ingredientId = request.IngredientId,
                            name = request.Name.Trim(),
                            unit = request.UnitId
                        }
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = message
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateIngredient API: {ex.Message}");

                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi cập nhật nguyên liệu",
                    error = ex.Message
                });
            }
        }

        [HttpGet("ExportTransactions")]
        public async Task<ActionResult<IEnumerable<object>>> GetExportTransactions()
        {
            try
            {
                var transactions = await _stockTransactionService.GetExportTransactionsAsync();

                var result = transactions.Select(t => new
                {
                    id = t.TransactionId,
                    ingredientId = t.IngredientId,
                    ingredientName = t.IngredientName ?? "N/A",
                    type = t.Type,
                    quantity = t.Quantity,
                    date = t.TransactionDate?.ToString("yyyy-MM-ddTHH:mm:ss"),
                    note = t.Note ?? "",
                    batchId = t.BatchId,
                    batchName = t.BatchName ?? $"Lô {t.BatchId}"
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi lấy dữ liệu xuất kho",
                    error = ex.Message
                });
            }


        }


        // API riêng để kiểm tra TRƯỚC
        [HttpGet]
        [Route("api/Audit/CheckStatus/{batchId}")]
        public async Task<IActionResult> CheckAuditStatus(int batchId)
        {
            var auditId = await _auditService.CheckExitsAuditStatus(batchId);

            return Ok(new
            {
                success = true,
                hasUnprocessedAudit = !string.IsNullOrEmpty(auditId),
                auditId = auditId
            });
        }


        [HttpPost("Audit/Create")]
        public async Task<IActionResult> Create([FromForm] AuditInventoryRequestDTO model)
        {
            try
            {
                // ✅ 1. VALIDATE INPUT CƠ BẢN
                if (string.IsNullOrWhiteSpace(model.PurchaseOrderId))
                    return BadRequest(new { success = false, message = "Thiếu mã lô (PO)" });

                if (string.IsNullOrWhiteSpace(model.IngredientCode))
                    return BadRequest(new { success = false, message = "Thiếu mã nguyên liệu" });

                if (model.CreatorId <= 0)
                    return BadRequest(new { success = false, message = "Thiếu thông tin người tạo đơn" });

                if (string.IsNullOrWhiteSpace(model.Reason))
                    return BadRequest(new { success = false, message = "Vui lòng nhập lý do kiểm kê" });

                if (model.AdjustmentQuantity < 0)
                    return BadRequest(new { success = false, message = "Số lượng điều chỉnh phải lớn hơn 0" });

                if (string.IsNullOrWhiteSpace(model.CreatorName) ||
                    string.IsNullOrWhiteSpace(model.CreatorPosition) ||
                    string.IsNullOrWhiteSpace(model.CreatorPhone))
                    return BadRequest(new { success = false, message = "Thông tin người tạo đơn không đầy đủ" });

                // ✅ 2. XỬ LÝ FILE ẢNH
                string? imagePath = null;
                if (model.ImageFile is { Length: > 0 })
                {
                    imagePath = await _cloudinaryService.UploadImageAsync(model.ImageFile, "audit_proofs");
                }

                var resultCheck = await _auditService.CheckExitsAuditStatus(model.BatchId);

                if (resultCheck == null)
                {
                    // ✅ 3. TẠO AUDIT ID (theo format tùy chọn)

                    string auditId = await GenerateAuditId();


                    // ✅ 4. TẠO ĐỐI TƯỢNG
                    var auditRecord = new AuditInventory
                    {
                        AuditId = auditId,  // ✅ GÁN AuditId đã tạo
                        BatchId = model.BatchId,
                        PurchaseOrderId = model.PurchaseOrderId.Trim(),
                        IngredientCode = model.IngredientCode.Trim(),
                        OriginalQuantity = model.OriginalQuantity,
                        ingredientName = model.IngredientName.Trim(),
                        unit = model.Unit,
                        ExpiryDate = model.ExpiryDate,

                        // Thông tin người tạo
                        CreatorId = model.CreatorId,
                        CreatedAt = model.CreatedAt != default ? model.CreatedAt : DateTime.Now,
                        CreatorName = model.CreatorName.Trim(),
                        CreatorPosition = model.CreatorPosition.Trim(),
                        CreatorPhone = model.CreatorPhone.Trim(),

                        // Thông tin kiểm kê
                        Reason = model.Reason.Trim(),
                        AdjustmentQuantity = model.AdjustmentQuantity,
                        IsAddition = model.IsAddition,
                        IngredientStatus = model.IngredientStatus?.Trim() ?? "Normal",
                        AuditStatus = "processing",
                        ImagePath = imagePath,

                        // Người xác nhận
                        ConfirmerId = null,
                        ConfirmedAt = null,
                        ConfirmerName = null,
                        ConfirmerPosition = null,
                        ConfirmerPhone = null
                    };

                    // ✅ 5. LƯU VÀO DATABASE
                    var result = await _auditService.CreateAuditAsync(auditRecord);

                    if (!result)
                    {
                        return StatusCode(500, new
                        {
                            success = false,
                            message = "Không thể lưu đơn kiểm kê. Vui lòng thử lại."
                        });
                    }

                    // ✅ 6. TRẢ VỀ KẾT QUẢ
                    return Ok(new
                    {
                        success = true,
                        message = "Tạo đơn kiểm kê thành công!",
                        data = new
                        {
                            AuditId = auditRecord.AuditId,  // ✅ Trả về AuditId dạng string
                            PurchaseOrderId = auditRecord.PurchaseOrderId,
                            IngredientCode = auditRecord.IngredientCode,
                            ingredientName = auditRecord.ingredientName,
                            unit = auditRecord.unit,
                            OriginalQuantity = auditRecord.OriginalQuantity,
                            AdjustmentQuantity = auditRecord.AdjustmentQuantity,
                            IsAddition = auditRecord.IsAddition,
                            AuditStatus = auditRecord.AuditStatus,
                            CreatedAt = auditRecord.CreatedAt,
                            CreatorName = auditRecord.CreatorName,
                            ImagePath = imagePath
                        }
                    });
                }
                else
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Tạo đơn không thành công, lô hàng này đang có đơn xử lý!",
                        data = new
                        {
                            resultCheck = resultCheck
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi trong quá trình xử lý đơn kiểm kê.",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// GET: api/InventoryIngredient/kitchen-pickup?categoryName=Xào
        /// Lấy danh sách nguyên liệu cần lấy từ lô hàng cho các món đang nấu, filter theo category
        /// </summary>
        [HttpGet("kitchen-pickup")]
        public async Task<IActionResult> GetKitchenIngredientPickup([FromQuery] string? categoryName = null)
        {
            try
            {
                var pickupList = await _inventoryIngredientService.GetIngredientPickupListAsync(categoryName);
                return Ok(new { success = true, data = pickupList });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private async Task<string> GenerateAuditId()
        {
            // Format: AUD-YYYYMMDD-XXXX
            var today = DateTime.Now.ToString("yyyyMMdd");
            var prefix = $"AUD-{today}-";

            // Đếm số đơn trong ngày
            var count = await _auditService.CountAuditAsync(string.Format(prefix, today));

            var sequence = (count + 1).ToString().PadLeft(4, '0');

            return $"{prefix}{sequence}";
            // Ví dụ: AUD-20241123-0001
        }
    }
}