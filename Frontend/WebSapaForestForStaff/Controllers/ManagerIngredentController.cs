using Azure;
using BusinessAccessLayer.DTOs.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;
using WebSapaForestForStaff.DTOs;
using WebSapaForestForStaff.DTOs.Inventory;

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

            try
            {
                var response = await _httpClient.GetAsync("api/InventoryIngredient");

                List<InventoryIngredientDTO> ingredientList;

                if (!response.IsSuccessStatusCode)
                {
                    // Trả về danh sách rỗng thay vì NotFound
                    ingredientList = new List<InventoryIngredientDTO>();
                }
                else
                {
                    var json = await response.Content.ReadAsStringAsync();
                    ingredientList = JsonConvert.DeserializeObject<List<InventoryIngredientDTO>>(json)
                                     ?? new List<InventoryIngredientDTO>();
                }

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
            catch (Exception ex)
            {
                // Xử lý lỗi: trả về model rỗng với thông báo
                var model = new InventoryPagedViewModel
                {
                    Ingredients = new List<InventoryIngredientDTO>(),
                    CurrentPage = 1,
                    ItemsPerPage = itemsPerPage,
                    TotalItems = 0
                };

                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách nguyên liệu: " + ex.Message;
                return View("~/Views/Inventory/ManagerIngredent.cshtml", model);
            }
        }


        [HttpPost]
        [HttpGet]
        public async Task<IActionResult> FilterIngredent(
            DateTime? fromDate,
            DateTime? toDate,
            string searchIngredent,
            int page = 1)
        {
            int itemsPerPage = 10;

            // Nếu không có filter, gọi DisplayIngredent
            if (fromDate == null && toDate == null &&
                string.IsNullOrEmpty(searchIngredent))
            {
                return await DisplayIngredent(page);
            }

            try
            {
                // Gói dữ liệu cần gửi sang API
                var requestData = new
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    SearchIngredent = searchIngredent
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(requestData),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync("api/InventoryIngredient/filter", content);

                List<InventoryIngredientDTO> ingredientList;

                if (!response.IsSuccessStatusCode)
                {
                    // Trả về danh sách rỗng thay vì NotFound
                    ingredientList = new List<InventoryIngredientDTO>();
                    TempData["InfoMessage"] = "Không tìm thấy nguyên liệu nào phù hợp với điều kiện tìm kiếm.";
                }
                else
                {
                    var json = await response.Content.ReadAsStringAsync();
                    ingredientList = JsonConvert.DeserializeObject<List<InventoryIngredientDTO>>(json)
                                     ?? new List<InventoryIngredientDTO>();

                    if (ingredientList.Count == 0)
                    {
                        TempData["InfoMessage"] = "Không tìm thấy nguyên liệu nào phù hợp với điều kiện tìm kiếm.";
                    }
                }

                // Phân trang
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
                    TotalItems = totalItems,
                    FromDate = fromDate,
                    ToDate = toDate,
                    SearchIngredent = searchIngredent
                };

                return View("~/Views/Inventory/ManagerIngredent.cshtml", model);
            }
            catch (Exception ex)
            {
                // Xử lý lỗi: trả về model rỗng với thông báo
                var model = new InventoryPagedViewModel
                {
                    Ingredients = new List<InventoryIngredientDTO>(),
                    CurrentPage = 1,
                    ItemsPerPage = itemsPerPage,
                    TotalItems = 0,
                    FromDate = fromDate,
                    ToDate = toDate,
                    SearchIngredent = searchIngredent
                };

                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tìm kiếm: " + ex.Message;
                return View("~/Views/Inventory/ManagerIngredent.cshtml", model);
            }
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


        [HttpPost]
        public async Task<IActionResult> UpdateIngredient([FromBody] UpdateIngredientRequest request)
        {
            try
            {
                // Validate input
                if (request.IngredientId <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "IngredientId không hợp lệ"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Tên nguyên liệu không được để trống"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Unit))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Đơn vị tính không được để trống"
                    });
                }

                // Tạo content để gửi đến API
                var content = new StringContent(
                    JsonConvert.SerializeObject(request),
                    Encoding.UTF8,
                    "application/json"
                );

                // Gọi API backend
                var response = await _httpClient.PutAsync(
                    "api/InventoryIngredient/UpdateIngredient",
                    content
                );

                // Đọc response từ API
                var result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = JsonConvert.DeserializeObject<dynamic>(result);
                    return Json(new
                    {
                        success = false,
                        message = errorResponse?.message?.ToString() ?? "Không thể cập nhật nguyên liệu"
                    });
                }

                var apiResponse = JsonConvert.DeserializeObject<dynamic>(result);

                return Json(new
                {
                    success = true,
                    message = apiResponse?.message?.ToString() ?? "Cập nhật nguyên liệu thành công",
                    data = apiResponse?.data
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