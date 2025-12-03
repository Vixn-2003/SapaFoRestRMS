using Microsoft.AspNetCore.Mvc;
using WebSapaForestForStaff.DTOs.ShiftManagement;
using WebSapaForestForStaff.Services.Api;

namespace WebSapaForestForStaff.Controllers
{
    public class CashierFlowController : Controller
    {
        private readonly IShiftManagementApiService _shiftApiService;
        private readonly ILogger<CashierFlowController> _logger;

        public CashierFlowController(
            IShiftManagementApiService shiftApiService,
            ILogger<CashierFlowController> logger)
        {
            _shiftApiService = shiftApiService;
            _logger = logger;
        }

        // GET: /CashierFlow/ShiftManagement
        [HttpGet]
        public async Task<IActionResult> ShiftManagement()
        {
            try
            {
                // Load current shift data from API
                var currentShift = await _shiftApiService.GetCurrentShiftAsync();

                // Pass data to view
                return View("~/Views/CashierFlow/ShiftManagement/Index.cshtml", currentShift);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading shift management page");

                // Return view with empty model if API fails
                return View("~/Views/CashierFlow/ShiftManagement/Index.cshtml", new ShiftDashboardDto());
            }
        }

        // API endpoints for AJAX calls (optional - if you want server-side API)

        [HttpPost]
        public async Task<IActionResult> OpenShift([FromBody] OpenShiftRequestDto request)
        {
            try
            {
                var result = await _shiftApiService.OpenShiftAsync(request);
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening shift");
                return Json(new ShiftResponseDto
                {
                    Success = false,
                    Message = $"Lỗi: {ex.Message}"
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CloseShift([FromBody] CloseShiftRequestDto request)
        {
            try
            {
                var result = await _shiftApiService.CloseShiftAsync(request);
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing shift");
                return Json(new ShiftResponseDto
                {
                    Success = false,
                    Message = $"Lỗi: {ex.Message}"
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> HandoverShift([FromBody] HandoverShiftRequestDto request)
        {
            try
            {
                var result = await _shiftApiService.HandoverShiftAsync(request);
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handing over shift");
                return Json(new ShiftResponseDto
                {
                    Success = false,
                    Message = $"Lỗi: {ex.Message}"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCurrentShift()
        {
            try
            {
                var currentShift = await _shiftApiService.GetCurrentShiftAsync();
                return Json(currentShift);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current shift");
                return Json(new ShiftDashboardDto());
            }
        }
    }
}

