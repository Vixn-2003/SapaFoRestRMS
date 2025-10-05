using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using WebSapaForestForStaff.Models;

namespace WebSapaForestForStaff.Controllers
{
    public class SystemLogoController : Controller
    {
        private readonly HttpClient _client;
        private readonly string _apiUrl = "https://localhost:7096/api/SystemLogo";

        public SystemLogoController(HttpClient client)
        {
            _client = client;
        }

        // ================== DANH SÁCH LOGO (CÓ PHÂN TRANG) ==================
        public async Task<IActionResult> Index(string? search, string? status, int page = 1)
        {
            const int pageSize = 8;

            var response = await _client.GetAsync($"{_apiUrl}/all");
            if (!response.IsSuccessStatusCode)
                return View(new List<SystemLogoViewModel>());

            var json = await response.Content.ReadAsStringAsync();
            var logos = JsonConvert.DeserializeObject<List<SystemLogoViewModel>>(json) ?? new();

            // Bộ lọc theo trạng thái
            if (status == "Active")
                logos = logos.Where(l => l.IsActive).ToList();
            else if (status == "Inactive")
                logos = logos.Where(l => !l.IsActive).ToList();

            // Tìm kiếm theo tên
            if (!string.IsNullOrEmpty(search))
                logos = logos.Where(l => l.LogoName.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

            // Tính tổng trang
            int totalLogos = logos.Count;
            int totalPages = (int)Math.Ceiling(totalLogos / (double)pageSize);

            // Lấy danh sách theo trang
            logos = logos.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;

            return View(logos);
        }

        // ================== TẠO MỚI ==================
        [HttpGet]
        public IActionResult Create()
        {
            return View("Edit", new SystemLogoViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Create(SystemLogoViewModel model, IFormFile? file)
        {
            if (!ModelState.IsValid)
                return View("Edit", model);

            // Validate bắt buộc chọn file
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("File", "Vui lòng chọn một ảnh logo.");
                return View("Edit", model);
            }

            var formData = new MultipartFormDataContent
            {
                { new StringContent(model.LogoName), "LogoName" },
                { new StringContent(model.Description ?? ""), "Description" },
                { new StringContent(model.IsActive ? "true" : "false"), "IsActive" }
            };

            var fileContent = new StreamContent(file.OpenReadStream());
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            formData.Add(fileContent, "File", file.FileName);

            var response = await _client.PostAsync(_apiUrl, formData);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Thêm logo thành công!";
                return RedirectToAction("Index");
            }

            TempData["ErrorMessage"] = "Thêm logo thất bại.";
            return View("Edit", model);
        }

        // ================== CHỈNH SỬA ==================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var response = await _client.GetAsync($"{_apiUrl}/{id}");
            if (!response.IsSuccessStatusCode)
                return NotFound();

            var json = await response.Content.ReadAsStringAsync();
            var model = JsonConvert.DeserializeObject<SystemLogoViewModel>(json);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(SystemLogoViewModel model, IFormFile? file)
        {
            if (model.LogoId == 0)
            {
                TempData["ErrorMessage"] = "LogoId không hợp lệ!";
                return View(model);
            }

            if (!ModelState.IsValid)
                return View(model);

            int userId = 3; // sau này thay bằng user đăng nhập

            var formData = new MultipartFormDataContent
            {
                { new StringContent(model.LogoId.ToString()), "LogoId" },
                { new StringContent(model.LogoName ?? ""), "LogoName" },
                { new StringContent(model.Description ?? ""), "Description" },
                { new StringContent(model.IsActive ? "true" : "false"), "IsActive" }
            };

            if (file != null && file.Length > 0)
            {
                var fileStream = new StreamContent(file.OpenReadStream());
                fileStream.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                formData.Add(fileStream, "File", file.FileName);
            }
            else if (!string.IsNullOrEmpty(model.LogoUrl))
            {
                formData.Add(new StringContent(model.LogoUrl), "LogoUrl");
            }

            var response = await _client.PutAsync($"{_apiUrl}/{model.LogoId}?userId={userId}", formData);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Cập nhật logo thành công!";
                return RedirectToAction("Index");
            }

            var error = await response.Content.ReadAsStringAsync();
            TempData["ErrorMessage"] = $"Lỗi cập nhật: {response.StatusCode} - {error}";
            return View(model);
        }

        // ================== XÓA ==================
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _client.DeleteAsync($"{_apiUrl}/{id}");
            if (response.IsSuccessStatusCode)
                TempData["SuccessMessage"] = "Xóa logo thành công!";
            else
                TempData["ErrorMessage"] = "Xóa logo thất bại!";
            return RedirectToAction("Index");
        }
    }
}
