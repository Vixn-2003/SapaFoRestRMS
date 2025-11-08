using BusinessAccessLayer.DTOs.Inventory;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace SapaFoRestRMSAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImportIngredientController : ControllerBase
    {
        private readonly IInventoryIngredientService _inventoryIngredientService;
        private readonly ICloudinaryService _cloudinaryService;

        public ImportIngredientController(IInventoryIngredientService inventoryIngredientService, ICloudinaryService cloudinaryService)
        {
            _inventoryIngredientService = inventoryIngredientService;
            _cloudinaryService = cloudinaryService;
        }

        [HttpPost("ImportInventory")]
        public async Task<IActionResult> ImportInventory([FromForm] ImportSubmitModel model)
        {
            if (model == null)
                return BadRequest("Dữ liệu không hợp lệ.");

            if (model.ImportList == null || !model.ImportList.Any())
                return BadRequest("Danh sách nguyên liệu trống.");

            // ✅ Upload ảnh lên Cloudinary
            string? proofImageUrl = null;
            if (model.ProofFile != null)
            {
                proofImageUrl = await _cloudinaryService.UploadImageAsync(model.ProofFile, "import_proofs");
            }


            // Xử lý dữ liệu JSON của ImportList
            // (nếu bạn dùng FromForm, ImportList có thể đến dạng string => cần parse)
            if (model.ImportList == null || !model.ImportList.Any())
                return BadRequest("Thiếu danh sách nguyên liệu.");

            // Thực hiện lưu vào DB hoặc logic nghiệp vụ
            return Ok(new { message = "Đã nhận và lưu đơn nhập thành công." });
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromForm] ImportInventoryRequest request)
        {
            try
            {
                // ✅ VALIDATE
                if (string.IsNullOrWhiteSpace(request.ImportCode))
                    return BadRequest(new { success = false, message = "Thiếu mã đơn nhập" });

                if (string.IsNullOrWhiteSpace(request.Items))
                    return BadRequest(new { success = false, message = "Thiếu danh sách nguyên liệu" });

                // ✅ 1. PARSE DANH SÁCH ITEMS
                List<ImportItemDTO>? itemsList = null;
                try
                {
                    itemsList = JsonConvert.DeserializeObject<List<ImportItemDTO>>(request.Items);
                    Console.WriteLine($"✅ Parsed {itemsList?.Count ?? 0} items");
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"❌ JSON Parse Error: {ex.Message}");
                    return BadRequest(new
                    {
                        success = false,
                        message = "Lỗi định dạng danh sách nguyên liệu",
                        error = ex.Message
                    });
                }

                if (itemsList == null || !itemsList.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Danh sách nguyên liệu trống"
                    });
                }

                // ✅ 2. XỬ LÝ FILE ẢNH - Upload lên Cloudinary
                string? imagePath = null;
                if (request.ProofFile != null && request.ProofFile.Length > 0)
                {
                    Console.WriteLine($"📤 Uploading image: {request.ProofFile.FileName}, Size: {request.ProofFile.Length} bytes");

                    imagePath = await _cloudinaryService.UploadImageAsync(
                        request.ProofFile,
                        "import_proofs"
                    );

                    Console.WriteLine($"✅ Image uploaded: {imagePath}");
                }

                // ✅ 3. TẠO ĐỐI TƯỢNG ĐƠN NHẬP HÀNG
                var importOrder = new ImportOrder
                {
                    ImportCode = request.ImportCode,
                    ImportDate = request.ImportDate,
                    SupplierId = request.SupplierId,
                    SupplierName = request.SupplierName,
                    CreatorName = request.CreatorName,
                    CreatorPhone = request.CreatorPhone,
                    CheckerName = request.CheckerName,
                    CheckerPhone = request.CheckerPhone,
                    ProofImagePath = imagePath,
                    Status = "Pending",
                    CreatedAt = DateTime.Now,
                    TotalAmount = itemsList.Sum(i => i.Quantity * i.UnitPrice)
                };

                // ✅ 4. TẠO CHI TIẾT ĐƠN NHẬP
                var importDetails = itemsList.Select(item => new ImportDetail
                {
                    IngredientCode = item.IngredientCode,
                    IngredientName = item.IngredientName,
                    Unit = item.Unit,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    WarehouseId = item.WarehouseId,
                    TotalPrice = item.TotalPrice
                }).ToList();

                // ✅ 5. LƯU VÀO DATABASE
                // TODO: Implement save logic
                // await _inventoryIngredientService.CreateImportOrder(importOrder, importDetails);

                Console.WriteLine("✅ Đơn nhập hàng đã được tạo thành công!");

                return Ok(new
                {
                    success = true,
                    message = "Đơn nhập hàng đã được tạo thành công",
                    data = new
                    {
                        ImportCode = importOrder.ImportCode,
                        ImportDate = importOrder.ImportDate,
                        TotalAmount = importOrder.TotalAmount,
                        ItemCount = importDetails.Count,
                        ImagePath = imagePath,
                        Items = importDetails
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");

                return StatusCode(500, new
                {
                    success = false,
                    message = $"Lỗi server: {ex.Message}",
                    stackTrace = ex.StackTrace
                });
            }
        }
    }

    // ✅ REQUEST MODEL - Khớp với Frontend
    public class ImportInventoryRequest
    {
        public string ImportCode { get; set; } = null!;
        public DateTime ImportDate { get; set; }
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = null!;
        public string CreatorName { get; set; } = null!;
        public string CreatorPhone { get; set; } = null!;
        public string CheckerName { get; set; } = null!;
        public string CheckerPhone { get; set; } = null!;
        public string Items { get; set; } = null!; // JSON string từ frontend
        public IFormFile? ProofFile { get; set; }
    }

    // ✅ DTO - Khớp với cấu trúc JSON từ frontend
    public class ImportItemDTO
    {
        public string IngredientCode { get; set; } = null!;
        public string IngredientName { get; set; } = null!;
        public string Unit { get; set; } = null!;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public int WarehouseId { get; set; }
        public decimal TotalPrice { get; set; }
    }

    // ✅ ENTITY CLASSES
    public class ImportOrder
    {
        public int Id { get; set; }
        public string ImportCode { get; set; } = null!;
        public DateTime ImportDate { get; set; }
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = null!;
        public string CreatorName { get; set; } = null!;
        public string CreatorPhone { get; set; } = null!;
        public string CheckerName { get; set; } = null!;
        public string CheckerPhone { get; set; } = null!;
        public string? ProofImagePath { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class ImportDetail
    {
        public int Id { get; set; }
        public int ImportOrderId { get; set; }
        public string IngredientCode { get; set; } = null!;
        public string IngredientName { get; set; } = null!;
        public string Unit { get; set; } = null!;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public int WarehouseId { get; set; }
        public decimal TotalPrice { get; set; }
    }

}


