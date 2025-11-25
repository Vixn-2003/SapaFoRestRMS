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
                var responseUnit = await _httpClient.GetAsync("api/Unit");

                List<InventoryIngredientDTO> ingredientList;
                List<UnitDTO> unitList;

                // ✅ XỬ LÝ UNIT TRƯỚC
                if (!responseUnit.IsSuccessStatusCode)
                {
                    unitList = new List<UnitDTO>();
                }
                else
                {
                    var jsonU = await responseUnit.Content.ReadAsStringAsync();
                    unitList = JsonConvert.DeserializeObject<List<UnitDTO>>(jsonU)
                                     ?? new List<UnitDTO>();
                }

                if (!response.IsSuccessStatusCode)
                {
                    ingredientList = new List<InventoryIngredientDTO>();
                }
                else
                {
                    var json = await response.Content.ReadAsStringAsync();
                    ingredientList = JsonConvert.DeserializeObject<List<InventoryIngredientDTO>>(json)
                                     ?? new List<InventoryIngredientDTO>();

                    // ✅ MAP UNIT VÀO INGREDIENT
                    foreach (var ingredient in ingredientList)
                    {
                        if (ingredient.UnitId.HasValue)
                        {
                            ingredient.Unit = unitList.FirstOrDefault(u => u.UnitId == ingredient.UnitId.Value)
                                              ?? new UnitDTO();
                        }
                    }
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
                    TotalItems = totalItems,
                    Units = unitList // ✅ THÊM DANH SÁCH UNIT VÀO MODEL
                };

                return View("~/Views/Inventory/ManagerIngredent.cshtml", model);
            }
            catch (Exception ex)
            {
                var model = new InventoryPagedViewModel
                {
                    Ingredients = new List<InventoryIngredientDTO>(),
                    CurrentPage = 1,
                    ItemsPerPage = itemsPerPage,
                    TotalItems = 0,
                    Units = new List<UnitDTO>() // ✅ THÊM
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

            if (fromDate == null && toDate == null &&
                string.IsNullOrEmpty(searchIngredent))
            {
                return await DisplayIngredent(page);
            }

            try
            {
                // ✅ THÊM: Load Unit list
                var responseUnit = await _httpClient.GetAsync("api/Unit");
                List<UnitDTO> unitList;

                if (!responseUnit.IsSuccessStatusCode)
                {
                    unitList = new List<UnitDTO>();
                }
                else
                {
                    var jsonU = await responseUnit.Content.ReadAsStringAsync();
                    unitList = JsonConvert.DeserializeObject<List<UnitDTO>>(jsonU)
                                     ?? new List<UnitDTO>();
                }

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
                    ingredientList = new List<InventoryIngredientDTO>();
                    TempData["InfoMessage"] = "Không tìm thấy nguyên liệu nào phù hợp với điều kiện tìm kiếm.";
                }
                else
                {
                    var json = await response.Content.ReadAsStringAsync();
                    ingredientList = JsonConvert.DeserializeObject<List<InventoryIngredientDTO>>(json)
                                     ?? new List<InventoryIngredientDTO>();

                    // ✅ MAP UNIT VÀO INGREDIENT
                    foreach (var ingredient in ingredientList)
                    {
                        if (ingredient.UnitId.HasValue)
                        {
                            ingredient.Unit = unitList.FirstOrDefault(u => u.UnitId == ingredient.UnitId.Value)
                                              ?? new UnitDTO();
                        }
                    }

                    if (ingredientList.Count == 0)
                    {
                        TempData["InfoMessage"] = "Không tìm thấy nguyên liệu nào phù hợp với điều kiện tìm kiếm.";
                    }
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
                    TotalItems = totalItems,
                    FromDate = fromDate,
                    ToDate = toDate,
                    SearchIngredent = searchIngredent,
                    Units = unitList // ✅ THÊM
                };

                return View("~/Views/Inventory/ManagerIngredent.cshtml", model);
            }
            catch (Exception ex)
            {
                var model = new InventoryPagedViewModel
                {
                    Ingredients = new List<InventoryIngredientDTO>(),
                    CurrentPage = 1,
                    ItemsPerPage = itemsPerPage,
                    TotalItems = 0,
                    FromDate = fromDate,
                    ToDate = toDate,
                    SearchIngredent = searchIngredent,
                    Units = new List<UnitDTO>() // ✅ THÊM
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

                if (request.UnitId == null)
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
            public bool IsActive { get; set; }
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


        // Trong ManagerIngredentController.cs
        [HttpGet]
        public async Task<IActionResult> CheckAuditStatus(int batchId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/InventoryIngredient/api/Audit/CheckStatus/{batchId}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<dynamic>(content);

                    return Ok(new
                    {
                        success = true,
                        hasUnprocessedAudit = (bool)result.hasUnprocessedAudit,
                        auditId = (string)result.auditId
                    });
                }

                return BadRequest(new { success = false, message = "Không thể kiểm tra trạng thái đơn kiểm kê" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("api/Audit/SubmitAudit")]
        public async Task<IActionResult> SubmitAudit([FromForm] AuditInventoryRequestDTO request)
        {
            try
            {
                // ✅ 1. VALIDATE DỮ LIỆU
                if (string.IsNullOrWhiteSpace(request.PurchaseOrderId))
                    return BadRequest(new { success = false, message = "Mã lô không hợp lệ" });

                if (string.IsNullOrWhiteSpace(request.Reason))
                    return BadRequest(new { success = false, message = "Vui lòng nhập lý do kiểm kê" });

                if (string.IsNullOrWhiteSpace(request.CreatorName) ||
                    string.IsNullOrWhiteSpace(request.CreatorPosition) ||
                    string.IsNullOrWhiteSpace(request.CreatorPhone))
                    return BadRequest(new { success = false, message = "Thông tin người tạo đơn không đầy đủ" });

                if (request.ImageFile == null || request.ImageFile.Length == 0)
                    return BadRequest(new { success = false, message = "Thiếu hình ảnh minh chứng" });

                // TODO: Lấy UserId từ Session/Claims
                int currentUserId = 2; // Tạm thời hardcode

                Console.WriteLine($"Processing audit for PO: {request.PurchaseOrderId}");

                // ✅ 2. TẠO MULTIPART FORM DATA ĐỂ GỬI SANG API BACKEND
                var formData = new MultipartFormDataContent();

                // Thêm các field thông tin cơ bản
                formData.Add(new StringContent(request.BatchId.ToString()), "BatchId");
                formData.Add(new StringContent(request.PurchaseOrderId), "PurchaseOrderId");
                formData.Add(new StringContent(request.IngredientCode ?? ""), "IngredientCode");
                formData.Add(new StringContent(request.IngredientName ?? ""), "IngredientName");
                formData.Add(new StringContent(request.Unit ?? ""), "Unit");
                formData.Add(new StringContent(request.OriginalQuantity.ToString()), "OriginalQuantity");

                // Xử lý ExpiryDate (nullable)
                if (request.ExpiryDate.HasValue)
                {
                    formData.Add(new StringContent(request.ExpiryDate.Value.ToString("yyyy-MM-dd")), "ExpiryDate");
                }

                // Thông tin người tạo đơn
                formData.Add(new StringContent(currentUserId.ToString()), "CreatorId");
                formData.Add(new StringContent(request.CreatedAt.ToString("o")), "CreatedAt"); // ISO 8601 format
                formData.Add(new StringContent(request.CreatorName), "CreatorName");
                formData.Add(new StringContent(request.CreatorPosition), "CreatorPosition");
                formData.Add(new StringContent(request.CreatorPhone), "CreatorPhone");

                // Thông tin kiểm kê
                formData.Add(new StringContent(request.Reason), "Reason");
                formData.Add(new StringContent(request.AdjustmentQuantity.ToString()), "AdjustmentQuantity");
                formData.Add(new StringContent(request.IsAddition.ToString().ToLower()), "IsAddition");
                formData.Add(new StringContent(request.IngredientStatus ?? ""), "IngredientStatus");
                formData.Add(new StringContent("processing"), "AuditStatus"); // Mặc định là processing

                // ✅ 3. THÊM FILE ẢNH
                if (request.ImageFile != null && request.ImageFile.Length > 0)
                {
                    var fileStream = request.ImageFile.OpenReadStream();
                    var streamContent = new StreamContent(fileStream);
                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                        request.ImageFile.ContentType ?? "image/jpeg"
                    );
                    formData.Add(streamContent, "ImageFile", request.ImageFile.FileName);
                }

                Console.WriteLine("Sending audit data to API Backend...");

                // ✅ 4. GỬI SANG API BACKEND
                var response = await _httpClient.PostAsync("api/InventoryIngredient/Audit/Create", formData);

                Console.WriteLine($"API Response Status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API Success: {result}");
                    return Ok(new
                    {
                        success = true,
                        message = "Tạo đơn kiểm kê thành công!",
                        data = result
                    });
                }

                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Error: {error}");
                return StatusCode((int)response.StatusCode, new
                {
                    success = false,
                    message = $"Không thể tạo đơn kiểm kê: {error}"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Có lỗi xảy ra: {ex.Message}"
                });
            }
        }

       
    }
}