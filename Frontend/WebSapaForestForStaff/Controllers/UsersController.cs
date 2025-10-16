using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebSapaForestForStaff.DTOs;
using WebSapaForestForStaff.DTOs.UserManagement;
using WebSapaForestForStaff.Services;

namespace WebSapaForestForStaff.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly ApiService _apiService;

        public UsersController(ApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var users = await _apiService.GetUsersAsync();
                return View(users ?? new List<User>());
            }
            catch
            {
                TempData["ErrorMessage"] = "Lỗi khi tải danh sách người dùng";
                return View(new List<User>());
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var user = await _apiService.GetUserAsync(id);
                if (user == null)
                {
                    return NotFound();
                }
                return View(user);
            }
            catch
            {
                TempData["ErrorMessage"] = "Lỗi khi tải thông tin người dùng";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult CreateManager()
        {
            return View(new CreateManagerRequest());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateManager(CreateManagerRequest model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var success = await _apiService.CreateManagerAsync(model);
                if (success)
                {
                    TempData["SuccessMessage"] = "Tạo tài khoản quản lý thành công!";
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", "Lỗi khi tạo tài khoản quản lý");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi kết nối. Vui lòng thử lại sau");
                return View(model);
            }
        }

        [HttpGet]
        [Authorize(Roles = "Manager")]
        public IActionResult CreateStaff()
        {
            return View(new CreateStaffRequest());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> CreateStaff(CreateStaffRequest model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var success = await _apiService.CreateStaffAsync(model);
                if (success)
                {
                    TempData["SuccessMessage"] = "Tạo tài khoản nhân viên thành công!";
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", "Lỗi khi tạo tài khoản nhân viên");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi kết nối. Vui lòng thử lại sau");
                return View(model);
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var user = await _apiService.GetUserAsync(id);
                if (user == null)
                {
                    return NotFound();
                }
                return View(user);
            }
            catch
            {
                TempData["ErrorMessage"] = "Lỗi khi tải thông tin người dùng";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(User model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var success = await _apiService.UpdateUserAsync(model);
                if (success)
                {
                    TempData["SuccessMessage"] = "Cập nhật thông tin người dùng thành công!";
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", "Lỗi khi cập nhật thông tin người dùng");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi kết nối. Vui lòng thử lại sau");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _apiService.DeleteUserAsync(id);
                if (success)
                {
                    TempData["SuccessMessage"] = "Xóa người dùng thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Lỗi khi xóa người dùng";
                }
            }
            catch
            {
                TempData["ErrorMessage"] = "Lỗi kết nối. Vui lòng thử lại sau";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> ChangeStatus(int id, int status)
        {
            try
            {
                var success = await _apiService.ChangeUserStatusAsync(id, status);
                if (success)
                {
                    TempData["SuccessMessage"] = "Thay đổi trạng thái người dùng thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Lỗi khi thay đổi trạng thái người dùng";
                }
            }
            catch
            {
                TempData["ErrorMessage"] = "Lỗi kết nối. Vui lòng thử lại sau";
            }

            return RedirectToAction("Index");
        }
    }
}
