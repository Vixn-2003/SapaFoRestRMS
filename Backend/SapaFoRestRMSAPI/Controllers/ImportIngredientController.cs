using BusinessAccessLayer.DTOs.Inventory;
using BusinessAccessLayer.Services;
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
        private readonly IPurchaseOrderService _purchaseOrderService;
        private readonly ICloudinaryService _cloudinaryService;

        public ImportIngredientController(IInventoryIngredientService inventoryIngredientService, ICloudinaryService cloudinaryService, IPurchaseOrderService purchaseOrderService)
        {
            _inventoryIngredientService = inventoryIngredientService;
            _cloudinaryService = cloudinaryService;
            _purchaseOrderService = purchaseOrderService;
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
                //  1. VALIDATE INPUT CƠ BẢN
                if (string.IsNullOrWhiteSpace(request.ImportCode))
                    return BadRequest(new { success = false, message = "Thiếu mã đơn nhập" });

                if (request.SupplierId <= 0)
                    return BadRequest(new { success = false, message = "Thiếu thông tin nhà cung cấp" });

                if (request.CreatorId <= 0)
                    return BadRequest(new { success = false, message = "Thiếu thông tin người tạo đơn" });

                if (string.IsNullOrWhiteSpace(request.Items))
                    return BadRequest(new { success = false, message = "Thiếu danh sách nguyên liệu" });

                // 🧩 2. CHUYỂN DỮ LIỆU ITEMS TỪ JSON → DANH SÁCH
                List<ImportItemDTO>? itemsList;
                try
                {
                    itemsList = JsonConvert.DeserializeObject<List<ImportItemDTO>>(request.Items);
                    if (itemsList == null || !itemsList.Any())
                        return BadRequest(new { success = false, message = "Danh sách nguyên liệu trống" });
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($" JSON Parse Error: {ex.Message}");
                    return BadRequest(new
                    {
                        success = false,
                        message = "Lỗi định dạng danh sách nguyên liệu. Vui lòng kiểm tra lại JSON gửi lên.",
                        error = ex.Message
                    });
                }

                // 🧩 3. XỬ LÝ FILE ẢNH (UPLOAD CLOUDINARY)
                string? imagePath = null;
                if (request.ProofFile is { Length: > 0 })
                {
                    Console.WriteLine($"Uploading image: {request.ProofFile.FileName} ({request.ProofFile.Length} bytes)");
                    imagePath = await _cloudinaryService.UploadImageAsync(request.ProofFile, "import_proofs");
                    Console.WriteLine($"Image uploaded successfully: {imagePath}");
                }

                // 🧩 4. TẠO ĐỐI TƯỢNG ĐƠN NHẬP HÀNG (PurchaseOrder)
                var importOrder = new ImportOrder
                {
                    ImportCode = request.ImportCode.Trim(),
                    ImportDate = request.ImportDate,
                    SupplierId = request.SupplierId,
                    CreatorId = request.CreatorId,
                    //CheckId = request.CheckId,
                    ProofImagePath = imagePath,
                    Status = "Processing", 
                    CreatedAt = DateTime.Now,
                    TotalAmount = itemsList.Sum(i => i.Quantity * i.UnitPrice)
                };

                // 🧩 5. TẠO DANH SÁCH CHI TIẾT NHẬP HÀNG
                var importDetails = itemsList.Select(item => new ImportDetail
                {
                    IngredientId = item.IngredientId,
                    IngredientCode = item.IngredientCode.Trim(),
                    IngredientName = item.IngredientName.Trim(),
                    Unit = item.Unit,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    WarehouseId = item.WarehouseId,
                    TotalPrice = item.TotalPrice
                }).ToList();

                // 🧩 6. GỌI SERVICE XỬ LÝ LƯU DATABASE
                var result = await _purchaseOrderService.CreateImportOrderAsync(importOrder, importDetails);

                if (!result)
                {
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Không thể lưu đơn nhập hàng. Vui lòng thử lại."
                    });
                }

                // 🧩 7. PHẢN HỒI KẾT QUẢ THÀNH CÔNG
                Console.WriteLine("Đơn nhập hàng đã được tạo thành công!");

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
                // 🧩 8. XỬ LÝ NGOẠI LỆ
                Console.WriteLine($"❌ Exception: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");

                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi trong quá trình xử lý đơn nhập hàng.",
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

    }


}


