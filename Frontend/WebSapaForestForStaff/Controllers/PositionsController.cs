using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebSapaForestForStaff.DTOs.Positions;
using WebSapaForestForStaff.Services;

namespace WebSapaForestForStaff.Controllers
{
    [Authorize(Roles = "Admin,Manager,Owner")]
    public class PositionsController : Controller
    {
        private readonly ApiService _apiService;

        public PositionsController(ApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? searchTerm, int? status, int page = 1, int pageSize = 10)
        {
            var result = await _apiService.SearchPositionsAsync(new PositionSearchRequest
            {
                SearchTerm = searchTerm,
                Status = status,
                Page = page,
                PageSize = pageSize
            });

            ViewBag.SearchTerm = searchTerm;
            ViewBag.Status = status;
            return View(result);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new PositionCreateRequest());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PositionCreateRequest model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var ok = await _apiService.CreatePositionAsync(model);
            if (ok)
            {
                TempData["SuccessMessage"] = "Tạo vị trí thành công";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = "Không thể tạo vị trí. Tên có thể đã tồn tại.";
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var pos = await _apiService.GetPositionAsync(id);
            if (pos == null) return NotFound();
            var model = new PositionUpdateRequest
            {
                PositionId = pos.PositionId,
                PositionName = pos.PositionName,
                Description = pos.Description,
                Status = pos.Status
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PositionUpdateRequest model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var ok = await _apiService.UpdatePositionAsync(model);
            if (ok)
            {
                TempData["SuccessMessage"] = "Cập nhật vị trí thành công";
                return RedirectToAction(nameof(Index));
            }
            TempData["ErrorMessage"] = "Không thể cập nhật vị trí.";
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var pos = await _apiService.GetPositionAsync(id);
            if (pos == null) return NotFound();
            return View(pos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _apiService.DeletePositionAsync(id);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Xóa vị trí thành công" : "Không thể xóa vị trí.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int id, int status)
        {
            var ok = await _apiService.ChangePositionStatusAsync(id, status);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Đổi trạng thái thành công" : "Không thể đổi trạng thái.";
            return RedirectToAction(nameof(Index));
        }
    }
}
