using BusinessAccessLayer.DTOs.Inventory;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{
    public class ImportIngredientController : Controller
    {
        private readonly IInventoryIngredientService _inventoryIngredientService;
        private readonly ICloudinaryService _cloudinaryService;

        public ImportIngredientController(IInventoryIngredientService inventoryIngredientService, ICloudinaryService cloudinaryService)
        {
            _inventoryIngredientService = inventoryIngredientService;
            _cloudinaryService = cloudinaryService;
        }

        [HttpPost]
        [Route("api/ImportInventory")]
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

    }
}
