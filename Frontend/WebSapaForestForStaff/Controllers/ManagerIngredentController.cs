using Azure;
using BusinessAccessLayer.DTOs.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;
using WebSapaForestForStaff.DTOs;

namespace WebSapaForestForStaff.Controllers
{
    public class ManagerIngredentController : Controller
    {

        private readonly HttpClient _httpClient;
        List<InventoryIngredientDTO> listIngredient;

        public ManagerIngredentController(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://localhost:7096/");
        }
        public async Task<IActionResult> DisplayIngredent(int page = 1)
        {
            int itemsPerPage = 10;

            var response = await _httpClient.GetAsync("api/InventoryIngredient");
            if (!response.IsSuccessStatusCode)
            {
                return NotFound("Không tìm thấy nguyên liệu nào.");
            }

            var json = await response.Content.ReadAsStringAsync();
            var ingredientList = JsonConvert.DeserializeObject<List<InventoryIngredientDTO>>(json);

            var totalItems = ingredientList.Count;
            var pagedList = ingredientList
                .Skip((page - 1) * itemsPerPage)
                .Take(itemsPerPage)
                .ToList();

            var model = new InventoryPagedViewModel
            {
                Ingredients = pagedList,
                CurrentPage = page,
                ItemsPerPage = itemsPerPage,
                TotalItems = totalItems
            };

            return View("~/Views/Inventory/ManagerIngredent.cshtml", model);
        }


        [HttpPost]
        public async Task<IActionResult> FilterIngredent(DateTime? fromDate, DateTime? toDate, string selectedUnit, int page = 1)
        {
            int itemsPerPage = 10;

            // Gói dữ liệu cần gửi sang API
            var requestData = new
            {
                FromDate = fromDate,
                ToDate = toDate,
                SelectedUnit = selectedUnit
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(requestData),
                Encoding.UTF8,
                "application/json"
            );

            // Gửi POST request đến API filter
            var response = await _httpClient.PostAsync("api/InventoryIngredient/filter", content);

            if (!response.IsSuccessStatusCode)
            {
                return NotFound("Không tìm thấy nguyên liệu nào.");
            }

            // Đọc kết quả trả về
            var json = await response.Content.ReadAsStringAsync();
            var ingredientList = JsonConvert.DeserializeObject<List<InventoryIngredientDTO>>(json);

            // Phân trang
            var totalItems = ingredientList.Count;
            var pagedList = ingredientList
                .Skip((page - 1) * itemsPerPage)
                .Take(itemsPerPage)
                .ToList();

            // Trả lại dữ liệu và giữ nguyên bộ lọc
            var model = new InventoryPagedViewModel
            {
                Ingredients = pagedList,
                CurrentPage = page,
                ItemsPerPage = itemsPerPage,
                TotalItems = totalItems,

                // Giữ lại giá trị người dùng đã chọn để hiển thị lại
                FromDate = fromDate,
                ToDate = toDate,
                SelectedUnit = selectedUnit
            };

            return View("~/Views/Inventory/ManagerIngredent.cshtml", model);
        }


        [HttpGet]
        [Route("api/InventoryIngredient/BatchIngredient/{id}")]
        public async Task<IActionResult> GetBatchDetails(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/InventoryIngredient/BatchIngredient/{id}");

                if (!response.IsSuccessStatusCode)
                {
                    return NotFound(new { message = "Không tìm thấy dữ liệu lô nguyên liệu." });
                }

                var json = await response.Content.ReadAsStringAsync();

                // Deserialize to check data
                var batchList = JsonConvert.DeserializeObject<List<BatchIngredientDTO>>(json);

                if (batchList == null || batchList.Count == 0)
                {
                    return Ok(new List<object>());
                }

                // Return JSON directly to client
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Có lỗi xảy ra khi tải dữ liệu",
                    error = ex.Message
                });
            }
        }

        // Thêm vào ManagerIngredentController.cs

        [HttpPost]
        public async Task<IActionResult> UpdateBatchWarehouse([FromBody] UpdateBatchWarehouseRequest request)
        {
            try
            {
                var content = new StringContent(
                    JsonConvert.SerializeObject(request),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PutAsync(
                    "api/InventoryIngredient/UpdateBatchWarehouse",
                    content
                );

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Json(new
                    {
                        success = false,
                        message = $"Không thể cập nhật kho: {errorContent}"
                    });
                }

                var result = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeObject<dynamic>(result);

                return Json(new
                {
                    success = true,
                    message = "Cập nhật kho thành công"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Có lỗi xảy ra: {ex.Message}"
                });
            }
        }


        // Model class
        public class UpdateBatchWarehouseRequest
        {
            public int BatchId { get; set; }
            public int WarehouseId { get; set; }
        }

        // Thêm API endpoint để lấy danh sách kho
        [HttpGet]
        [Route("api/Warehouse/GetAll")]
        public async Task<IActionResult> GetAllWarehouses()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/Warehouse");

                if (!response.IsSuccessStatusCode)
                {
                    return NotFound(new { message = "Không tìm thấy danh sách kho" });
                }

                var json = await response.Content.ReadAsStringAsync();
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
