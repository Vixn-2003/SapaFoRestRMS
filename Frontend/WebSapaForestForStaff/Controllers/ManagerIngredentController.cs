using Azure;
using BusinessAccessLayer.DTOs.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;
using System.Text;
using WebSapaForestForStaff.DTOs;
using WebSapaForestForStaff.DTOs.Inventory;
using WebSapaForestForStaff.Services;

namespace WebSapaForestForStaff.Controllers
{
    public class ManagerIngredentController : Controller
    {

        private readonly HttpClient _httpClient;
        private readonly IngredientReportService _reportService;

        public ManagerIngredentController(HttpClient httpClient, IngredientReportService reportService)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://localhost:7096/");
            _reportService = reportService;
        }

        public async Task<IActionResult> DisplayIngredent()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/InventoryIngredient");
                var responseUnit = await _httpClient.GetAsync("api/Unit");

                List<InventoryIngredientDTO> ingredientList;
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

                if (!response.IsSuccessStatusCode)
                {
                    ingredientList = new List<InventoryIngredientDTO>();
                }
                else
                {
                    var json = await response.Content.ReadAsStringAsync();
                    ingredientList = JsonConvert.DeserializeObject<List<InventoryIngredientDTO>>(json)
                                     ?? new List<InventoryIngredientDTO>();

                    foreach (var ingredient in ingredientList)
                    {
                        if (ingredient.UnitId.HasValue)
                        {
                            ingredient.Unit = unitList.FirstOrDefault(u => u.UnitId == ingredient.UnitId.Value)
                                              ?? new UnitDTO();
                        }
                    }
                }

                var model = new InventoryPagedViewModel
                {
                    Ingredients = ingredientList, // Không skip/take nữa
                    Units = unitList
                };

                return View("~/Views/Inventory/ManagerIngredent.cshtml", model);
            }
            catch (Exception ex)
            {
                var model = new InventoryPagedViewModel
                {
                    Ingredients = new List<InventoryIngredientDTO>(),
                    Units = new List<UnitDTO>()
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
    string searchIngredent)
        {
            if (fromDate == null && toDate == null &&
                string.IsNullOrEmpty(searchIngredent))
            {
                return await DisplayIngredent();
            }

            try
            {
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

                var model = new InventoryPagedViewModel
                {
                    Ingredients = ingredientList, // Không phân trang
                    FromDate = fromDate,
                    ToDate = toDate,
                    SearchIngredent = searchIngredent,
                    Units = unitList
                };

                return View("~/Views/Inventory/ManagerIngredent.cshtml", model);
            }
            catch (Exception ex)
            {
                var model = new InventoryPagedViewModel
                {
                    Ingredients = new List<InventoryIngredientDTO>(),
                    FromDate = fromDate,
                    ToDate = toDate,
                    SearchIngredent = searchIngredent,
                    Units = new List<UnitDTO>()
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

        [HttpPost]
        [Route("api/Ingredient/ExportReport")]
        public async Task<IActionResult> ExportIngredientReport([FromBody] IngredientReportRequest request)
        {
            try
            {
                // Lấy thông tin người dùng từ Claims
                var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Nhân viên kho";
                var userPosition = User.FindFirst("Positions")?.Value ?? "Nhân viên kho";

                // Lấy dữ liệu từ API
                var response = await _httpClient.GetAsync("api/InventoryIngredient");
                if (!response.IsSuccessStatusCode)
                {
                    return BadRequest("Không thể lấy dữ liệu mặt hàng");
                }

                var content = await response.Content.ReadAsStringAsync();
                var allIngredients = JsonConvert.DeserializeObject<List<InventoryIngredientDTO>>(content)
                    ?? new List<InventoryIngredientDTO>();

                // Lấy thông tin kho
                var warehouseResponse = await _httpClient.GetAsync("api/Warehouse");
                List<dynamic> warehouses = new List<dynamic>();

                if (warehouseResponse.IsSuccessStatusCode)
                {
                    var warehouseContent = await warehouseResponse.Content.ReadAsStringAsync();
                    warehouses = JsonConvert.DeserializeObject<List<dynamic>>(warehouseContent) ?? new List<dynamic>();
                }

                // Xác định khoảng thời gian
                DateTime fromDate, toDate;
                switch (request.Period)
                {
                    case "today":
                        fromDate = DateTime.Today;
                        toDate = DateTime.Today.AddDays(1).AddSeconds(-1);
                        break;
                    case "7days":
                        fromDate = DateTime.Today.AddDays(-7);
                        toDate = DateTime.Today.AddDays(1).AddSeconds(-1);
                        break;
                    case "30days":
                        fromDate = DateTime.Today.AddDays(-30);
                        toDate = DateTime.Today.AddDays(1).AddSeconds(-1);
                        break;
                    default:
                        fromDate = DateTime.Today;
                        toDate = DateTime.Today.AddDays(1).AddSeconds(-1);
                        break;
                }

                // ✅ LẤY DANH SÁCH BATCH CHO TẤT CẢ MẶT HÀNG
                // ✅ LẤY DANH SÁCH BATCH CHO TẤT CẢ MẶT HÀNG
                foreach (var ingredient in allIngredients)
                {
                    try
                    {
                        var batchResponse = await _httpClient.GetAsync($"api/InventoryIngredient/BatchIngredient/{ingredient.IngredientId}");
                        if (batchResponse.IsSuccessStatusCode)
                        {
                            var batchJson = await batchResponse.Content.ReadAsStringAsync();
                            var batches = JsonConvert.DeserializeObject<List<BatchIngredientDTO>>(batchJson);

                            if (batches != null)
                            {
                                ingredient.Batches = batches.Select(b => new InventoryBatchDTO
                                {
                                    BatchId = b.BatchId,
                                    QuantityRemaining = b.QuantityRemaining,
                                    ExpiryDate = b.ExpiryDate,
                                    CreatedAt = b.CreatedAt,
                                    IsActive = b.IsActive,
                                    StockTransactions = new List<StockTransactionDTO>() // Tạm thời empty
                                }).ToList();
                            }
                            else
                            {
                                ingredient.Batches = new List<InventoryBatchDTO>();
                            }
                        }
                    }
                    catch
                    {
                        ingredient.Batches = new List<InventoryBatchDTO>();
                    }
                }

                // Tạo báo cáo
                var reportData = new IngredientReportDTO
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    CreatedBy = userName,
                    TotalIngredients = allIngredients.Count,
                    TotalBatches = allIngredients.Sum(i => i.Batches.Count),
                    TotalWarehouses = warehouses.Count,

                    // Đếm cảnh báo
                    OutOfStockCount = allIngredients.Count(i => i.TotalQuantity == 0),
                    LowStockCount = allIngredients.Count(i => i.IsLowStock),
                    BelowReorderCount = allIngredients.Count(i => i.IsBelowReorderLevel),
                    UrgentRestockCount = allIngredients.Count(i => i.NeedUrgentRestock),
                    ExpiredBatchCount = allIngredients.Sum(i => i.ExpiredBatchCount),
                    ExpiringSoonBatchCount = allIngredients.Sum(i => i.ExpiringSoonBatchCount),

                    // Danh sách cảnh báo - HẾT HÀNG
                    OutOfStockItems = allIngredients
                        .Where(i => i.TotalQuantity == 0)
                        .Select(i => new IngredientAlertDTO
                        {
                            IngredientCode = i.IngredientCode,
                            IngredientName = i.Name,
                            Unit = i.Unit?.UnitName ?? "",
                            CurrentQuantity = i.TotalQuantity
                        })
                        .ToList(),

                    // SẮP HẾT HÀNG
                    LowStockItems = allIngredients
                        .Where(i => i.IsLowStock && i.TotalQuantity > 0)
                        .Select(i => new IngredientAlertDTO
                        {
                            IngredientCode = i.IngredientCode,
                            IngredientName = i.Name,
                            Unit = i.Unit?.UnitName ?? "",
                            CurrentQuantity = i.TotalQuantity,
                            ReorderLevel = i.ReorderLevel
                        })
                        .ToList(),

                    // CẦN NHẬP GẤP
                    UrgentRestockItems = allIngredients
                        .Where(i => i.NeedUrgentRestock)
                        .Select(i => new IngredientAlertDTO
                        {
                            IngredientCode = i.IngredientCode,
                            IngredientName = i.Name,
                            Unit = i.Unit?.UnitName ?? "",
                            CurrentQuantity = i.TotalQuantity,
                            QuantityExcludingExpired = i.QuantityExcludingExpired,
                            ReorderLevel = i.ReorderLevel
                        })
                        .ToList(),

                    // LÔ HẾT HẠN
                    ExpiredBatches = allIngredients
                        .SelectMany(i => i.Batches
                            .Where(b => b.ExpiryDate.HasValue &&
                                       b.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue) < DateTime.Today &&
                                       b.QuantityRemaining > 0)
                            .Select(b => new BatchAlertDTO
                            {
                                BatchCode = $"BATCH-{b.BatchId}",
                                IngredientName = i.Name,
                                Unit = i.Unit?.UnitName ?? "",
                                QuantityRemaining = b.QuantityRemaining,
                                ImportDate = b.CreatedAt,
                                ExpiryDate = b.ExpiryDate?.ToDateTime(TimeOnly.MinValue),
                                WarehouseName = "Kho chính"
                            }))
                        .OrderBy(b => b.ExpiryDate)
                        .ToList(),

                    // LÔ SẮP HẾT HẠN (< 7 ngày)
                    ExpiringSoonBatches = allIngredients
                        .SelectMany(i => i.Batches
                            .Where(b => b.ExpiryDate.HasValue &&
                                       b.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue) >= DateTime.Today &&
                                       (b.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue) - DateTime.Today).TotalDays <= 7 &&
                                       b.QuantityRemaining > 0)
                            .Select(b => new BatchAlertDTO
                            {
                                BatchCode = $"BATCH-{b.BatchId}",
                                IngredientName = i.Name,
                                Unit = i.Unit?.UnitName ?? "",
                                QuantityRemaining = b.QuantityRemaining,
                                ImportDate = b.CreatedAt,
                                ExpiryDate = b.ExpiryDate?.ToDateTime(TimeOnly.MinValue),
                                WarehouseName = "Kho chính",
                                DaysLeft = (int)(b.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue) - DateTime.Today).TotalDays
                            }))
                        .OrderBy(b => b.DaysLeft)
                        .ToList(),

                    // TẤT CẢ MẶT HÀNG
                    AllIngredients = allIngredients
                        .OrderBy(i => i.IngredientCode)
                        .Select(i => new IngredientDetailDTO
                        {
                            IngredientCode = i.IngredientCode,
                            IngredientName = i.Name,
                            Unit = i.Unit?.UnitName ?? "",
                            TotalQuantity = i.TotalQuantity,
                            ReorderLevel = i.ReorderLevel,
                            Status = i.Status,
                            StatusText = i.StatusText
                        })
                        .ToList()
                };

                // Tạo PDF
                var pdfBytes = _reportService.GenerateIngredientReport(reportData);

                var fileName = $"BaoCaoMatHang_{fromDate:ddMMyyyy}_{toDate:ddMMyyyy}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating report: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return StatusCode(500, $"Lỗi khi tạo báo cáo: {ex.Message}");
            }

        }
            public class IngredientReportRequest
        {
            public string Period { get; set; } = "today";
        }
    }
 }


