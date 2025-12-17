using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WebSapaForestForStaff.DTOs.ShiftManagement;
using WebSapaForestForStaff.Services.Api;
using System;
using System.Threading.Tasks;
using System.Security.Claims;

namespace WebSapaForestForStaff.Controllers
{
    [Authorize(Roles = "Staff")]
    [Route("shift-management")]
    public class ShiftManagementController : Controller
    {
        private readonly IShiftManagementApiService _shiftApiService;
        private readonly ILogger<ShiftManagementController> _logger;

        public ShiftManagementController(
            IShiftManagementApiService shiftApiService,
            ILogger<ShiftManagementController> logger)
        {
            _shiftApiService = shiftApiService;
            _logger = logger;
        }

        private int GetCurrentStaffId()
        {
            // Try StaffId first (if available), then fall back to UserId
            var staffIdClaim = User.FindFirst("StaffId")?.Value;
            if (int.TryParse(staffIdClaim, out var staffId) && staffId > 0)
            {
                return staffId;
            }
            
            // Fallback to UserId (for staff users, UserId is their StaffId)
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private int GetCurrentPositionId()
        {
            var positionIdClaim = User.FindFirst("PositionId")?.Value;
            return int.TryParse(positionIdClaim, out var positionId) ? positionId : 0;
        }

        private bool IsCounterStaff()
        {
            return GetCurrentPositionId() == 2; // PositionId = 2 is Cashier/Counter Staff
        }

        // ========== DASHBOARD ==========

        /// <summary>
        /// UC121 - Dashboard (Index page)
        /// GET /shift-management
        /// </summary>
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var staffId = GetCurrentStaffId();
                var dashboard = await _shiftApiService.GetShiftDashboardAsync(staffId);
                return View(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                TempData["ErrorMessage"] = "Không thể tải thông tin dashboard.";
                return View(new ShiftDashboardDto { Status = "NoShift" });
            }
        }

        // ========== OPENING FLOW ==========

        /// <summary>
        /// UC125 - Step 1: Opening page (declare opening balance)
        /// GET /shift-management/opening
        /// </summary>
        [HttpGet("opening")]
        public async Task<IActionResult> Opening()
        {
            try
            {
                var staffId = GetCurrentStaffId();
                
                // Check if staff already has open shift
                var hasOpen = await _shiftApiService.HasOpenShiftAsync(staffId);
                if (hasOpen)
                {
                    TempData["ErrorMessage"] = "Bạn đang có ca làm việc đang mở.";
                    return RedirectToAction(nameof(Index));
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Opening");
                TempData["ErrorMessage"] = "Lỗi khi mở trang Opening.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: Declare opening balance
        /// </summary>
        [HttpPost("opening/declare")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeclareOpening([FromForm] decimal openingBalance, [FromForm] string? notes)
        {
            try
            {
                var staffId = GetCurrentStaffId();
                var request = new ShiftOpeningDeclareRequestDto
                {
                    StaffId = staffId,
                    OpeningBalance = openingBalance,
                    Notes = notes
                };

                var result = await _shiftApiService.DeclareOpeningBalanceAsync(request);

                if (!result.Success)
                {
                    TempData["ErrorMessage"] = result.Message;
                    return RedirectToAction(nameof(Opening));
                }

                TempData["SuccessMessage"] = "Khai báo số dư thành công!";
                TempData["ShiftId"] = result.Shift?.ShiftId;
                return RedirectToAction(nameof(OpeningDenominations), new { shiftId = result.Shift?.ShiftId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error declaring opening balance");
                TempData["ErrorMessage"] = "Lỗi khi khai báo số dư.";
                return RedirectToAction(nameof(Opening));
            }
        }

        /// <summary>
        /// UC126 - Step 2: Count opening denominations
        /// GET /shift-management/opening/denominations/{shiftId}
        /// </summary>
        [HttpGet("opening/denominations/{shiftId}")]
        public async Task<IActionResult> OpeningDenominations(int shiftId)
        {
            try
            {
                var shift = await _shiftApiService.GetShiftByIdAsync(shiftId);
                if (shift == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy ca làm việc.";
                    return RedirectToAction(nameof(Index));
                }

                ViewData["ShiftId"] = shiftId;
                ViewData["OpeningBalance"] = shift.OpeningBalance ?? 0;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OpeningDenominations");
                TempData["ErrorMessage"] = "Lỗi khi mở trang nhập mệnh giá.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: Submit opening denominations
        /// </summary>
        [HttpPost("opening/denominations")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitOpeningDenominations([FromBody] ShiftOpeningDenominationsRequestDto request)
        {
            try
            {
                var result = await _shiftApiService.SubmitOpeningDenominationsAsync(request);

                if (!result.Success)
                {
                    return Json(new { success = false, message = result.Message });
                }

                return Json(new { success = true, message = "Lưu mệnh giá thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting opening denominations");
                return Json(new { success = false, message = "Lỗi khi lưu mệnh giá." });
            }
        }

        /// <summary>
        /// UC127 - Step 3: Confirm shift opening
        /// GET /shift-management/opening/confirm/{shiftId}
        /// </summary>
        [HttpGet("opening/confirm/{shiftId}")]
        public async Task<IActionResult> OpeningConfirm(int shiftId)
        {
            try
            {
                var shift = await _shiftApiService.GetShiftByIdAsync(shiftId);
                if (shift == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy ca làm việc.";
                    return RedirectToAction(nameof(Index));
                }

                return View(shift);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OpeningConfirm");
                TempData["ErrorMessage"] = "Lỗi khi mở trang xác nhận.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: Confirm opening
        /// </summary>
        [HttpPost("opening/confirm")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmOpening([FromForm] int shiftId)
        {
            try
            {
                var staffId = GetCurrentStaffId();
                var shift = await _shiftApiService.GetShiftByIdAsync(shiftId);

                var request = new ShiftOpeningConfirmRequestDto
                {
                    ShiftId = shiftId,
                    StaffId = staffId,
                    OpeningBalance = shift.OpeningBalance ?? 0
                };

                var result = await _shiftApiService.ConfirmShiftOpeningAsync(request);

                if (!result.Success)
                {
                    TempData["ErrorMessage"] = result.Message;
                    return RedirectToAction(nameof(OpeningConfirm), new { shiftId });
                }

                TempData["SuccessMessage"] = "✅ Mở ca thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming opening");
                TempData["ErrorMessage"] = "Lỗi khi xác nhận mở ca.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ========== CLOSING FLOW ==========

        /// <summary>
        /// UC128 - Step 1: Count closing cash
        /// GET /shift-management/closing
        /// </summary>
        [HttpGet("closing")]
        public async Task<IActionResult> Closing()
        {
            try
            {
                var staffId = GetCurrentStaffId();
                var shift = await _shiftApiService.GetCurrentOpenShiftAsync(staffId);

                if (shift == null)
                {
                    TempData["ErrorMessage"] = "Không có ca làm việc nào đang mở.";
                    return RedirectToAction(nameof(Index));
                }

                return View(shift);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Closing");
                TempData["ErrorMessage"] = "Lỗi khi mở trang kết ca.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// UC128 - Step 2: Count denominations
        /// GET /shift-management/closing/denominations
        /// </summary>
        [HttpGet("closing/denominations")]
        public async Task<IActionResult> ClosingDenominations()
        {
            try
            {
                var staffId = GetCurrentStaffId();
                var shift = await _shiftApiService.GetCurrentOpenShiftAsync(staffId);

                if (shift == null)
                {
                    TempData["ErrorMessage"] = "Không có ca làm việc nào đang mở.";
                    return RedirectToAction(nameof(Index));
                }

                return View(shift);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ClosingDenominations");
                TempData["ErrorMessage"] = "Lỗi khi mở trang kiểm kê.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: Submit closing denominations
        /// </summary>
        [HttpPost("closing/denominations")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitClosingDenominations([FromBody] ShiftDenominationsRequestDto request)
        {
            try
            {
                var result = await _shiftApiService.CountClosingCashAsync(request);

                if (!result.Success)
                {
                    return Json(new { success = false, message = result.Message });
                }

                // Calculate difference
                var difference = await _shiftApiService.CalculateDifferenceAsync(request.ShiftId, request.TotalAmount);

                return Json(new
                {
                    success = true,
                    message = "Lưu mệnh giá thành công!",
                    difference = new
                    {
                        openingBalance = difference.OpeningBalance,
                        totalRevenueCash = difference.TotalRevenueCash,
                        expectedClosingBalance = difference.ExpectedClosingBalance,
                        actualClosingBalance = difference.ActualClosingBalance,
                        difference = difference.Difference,
                        hasDifference = difference.HasDifference,
                        differenceType = difference.DifferenceType
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting closing denominations");
                return Json(new { success = false, message = "Lỗi khi lưu mệnh giá." });
            }
        }

        /// <summary>
        /// UC129-130 - Step 3: Review and add notes
        /// GET /shift-management/closing/review
        /// </summary>
        [HttpGet("closing/review")]
        public async Task<IActionResult> ClosingReview()
        {
            try
            {
                var staffId = GetCurrentStaffId();
                var shift = await _shiftApiService.GetCurrentOpenShiftAsync(staffId);

                if (shift == null)
                {
                    TempData["ErrorMessage"] = "Không có ca làm việc nào đang mở.";
                    return RedirectToAction(nameof(Index));
                }

                // Calculate difference
                var difference = await _shiftApiService.CalculateDifferenceAsync(
                    shift.ShiftId,
                    shift.ClosingBalance ?? 0);

                ViewData["Difference"] = difference;
                return View(shift);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ClosingReview");
                TempData["ErrorMessage"] = "Lỗi khi mở trang xem xét.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: Add closing notes
        /// </summary>
        [HttpPost("closing/notes")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddClosingNotes([FromBody] ShiftClosingNotesRequestDto request)
        {
            try
            {
                var success = await _shiftApiService.AddClosingNotesAsync(request);

                if (!success)
                {
                    return Json(new { success = false, message = "Không thể lưu ghi chú." });
                }

                return Json(new { success = true, message = "Lưu ghi chú thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding closing notes");
                return Json(new { success = false, message = "Lỗi khi lưu ghi chú." });
            }
        }

        /// <summary>
        /// UC131 - Step 4: Confirm closing
        /// POST /shift-management/closing/confirm
        /// </summary>
        [HttpPost("closing/confirm")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmClosing([FromBody] ShiftClosingConfirmRequestDto request)
        {
            try
            {
                var result = await _shiftApiService.ConfirmClosingAsync(request);

                if (!result.Success)
                {
                    return Json(new { success = false, message = result.Message });
                }

                return Json(new { success = true, message = "✅ Kết ca thành công!", redirectUrl = Url.Action(nameof(Index)) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming closing");
                return Json(new { success = false, message = "Lỗi khi xác nhận kết ca." });
            }
        }

        // ========== HANDOVER FLOW ==========

        /// <summary>
        /// UC132 - Step 1: Select handover staff
        /// GET /shift-management/handover
        /// </summary>
        [HttpGet("handover")]
        public async Task<IActionResult> Handover()
        {
            try
            {
                var staffId = GetCurrentStaffId();
                var shift = await _shiftApiService.GetCurrentOpenShiftAsync(staffId);

                if (shift == null)
                {
                    TempData["ErrorMessage"] = "Không có ca làm việc nào đang mở.";
                    return RedirectToAction(nameof(Index));
                }

                var availableStaff = await _shiftApiService.GetAvailableHandoverStaffAsync(staffId);
                ViewData["AvailableStaff"] = availableStaff;

                return View(shift);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Handover");
                TempData["ErrorMessage"] = "Lỗi khi mở trang giao ca.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// UC133 - Step 2: Add handover notes
        /// GET /shift-management/handover/notes/{toStaffId}
        /// </summary>
        [HttpGet("handover/notes/{toStaffId}")]
        public async Task<IActionResult> HandoverNotes(int toStaffId)
        {
            try
            {
                var staffId = GetCurrentStaffId();
                var shift = await _shiftApiService.GetCurrentOpenShiftAsync(staffId);

                if (shift == null)
                {
                    TempData["ErrorMessage"] = "Không có ca làm việc nào đang mở.";
                    return RedirectToAction(nameof(Index));
                }

                ViewData["ToStaffId"] = toStaffId;
                return View(shift);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HandoverNotes");
                TempData["ErrorMessage"] = "Lỗi khi mở trang ghi chú giao ca.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// UC134 - Step 3: Enter PIN
        /// GET /shift-management/handover/pin
        /// </summary>
        [HttpGet("handover/pin")]
        public async Task<IActionResult> HandoverPIN()
        {
            try
            {
                var staffId = GetCurrentStaffId();
                var shift = await _shiftApiService.GetCurrentOpenShiftAsync(staffId);

                if (shift == null)
                {
                    TempData["ErrorMessage"] = "Không có ca làm việc nào đang mở.";
                    return RedirectToAction(nameof(Index));
                }

                return View(shift);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HandoverPIN");
                TempData["ErrorMessage"] = "Lỗi khi mở trang xác thực.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: Verify PIN
        /// </summary>
        [HttpPost("handover/verify-pin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyHandoverPin([FromBody] ShiftHandoverPinRequestDto request)
        {
            try
            {
                var isValid = await _shiftApiService.VerifyHandoverPinAsync(request);

                if (!isValid)
                {
                    return Json(new { success = false, message = "Mã PIN không chính xác." });
                }

                return Json(new { success = true, message = "Xác thực thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying PIN");
                return Json(new { success = false, message = "Lỗi khi xác thực PIN." });
            }
        }

        /// <summary>
        /// UC135 - Step 4: Create next shift and complete handover
        /// POST /shift-management/handover/complete
        /// </summary>
        [HttpPost("handover/complete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteHandover([FromBody] ShiftHandoverCreateNextRequestDto request)
        {
            try
            {
                var result = await _shiftApiService.CreateNextShiftAfterHandoverAsync(request);

                if (!result.Success)
                {
                    return Json(new { success = false, message = result.Message });
                }

                return Json(new
                {
                    success = true,
                    message = "✅ Giao ca thành công!",
                    redirectUrl = Url.Action(nameof(HandoverComplete))
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing handover");
                return Json(new { success = false, message = "Lỗi khi hoàn tất giao ca." });
            }
        }

        /// <summary>
        /// Handover complete confirmation page
        /// GET /shift-management/handover/complete
        /// </summary>
        [HttpGet("handover/complete")]
        public IActionResult HandoverComplete()
        {
            return View();
        }

        // ========== HISTORY ==========

        /// <summary>
        /// UC136 - View shift history
        /// GET /shift-management/history
        /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> History([FromQuery] ShiftFilterDto filter)
        {
            try
            {
                var staffId = GetCurrentStaffId();
                
                if (!filter.StaffId.HasValue)
                {
                    filter.StaffId = staffId;
                }

                var history = await _shiftApiService.GetShiftHistoryAsync(filter);
                return View(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading history");
                TempData["ErrorMessage"] = "Không thể tải lịch sử ca làm việc.";
                return View(new ShiftHistoryListDto());
            }
        }

        /// <summary>
        /// UC137 - View shift details
        /// GET /shift-management/details/{shiftId}
        /// </summary>
        [HttpGet("details/{shiftId}")]
        public async Task<IActionResult> ShiftDetails(int shiftId)
        {
            try
            {
                var details = await _shiftApiService.GetShiftDetailsAsync(shiftId);
                return View(details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading shift details for {ShiftId}", shiftId);
                TempData["ErrorMessage"] = "Không thể tải chi tiết ca làm việc.";
                return RedirectToAction(nameof(History));
            }
        }

        /// <summary>
        /// UC138 - Export shift report
        /// GET /shift-management/export/{shiftId}
        /// </summary>
        [HttpGet("export/{shiftId}")]
        public async Task<IActionResult> ExportReport(int shiftId)
        {
            try
            {
                var pdfBytes = await _shiftApiService.ExportShiftReportAsync(shiftId);

                if (pdfBytes == null || pdfBytes.Length == 0)
                {
                    TempData["ErrorMessage"] = "Không thể xuất báo cáo.";
                    return RedirectToAction(nameof(ShiftDetails), new { shiftId });
                }

                return File(pdfBytes, "application/pdf", $"ShiftReport_{shiftId}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting report for shift {ShiftId}", shiftId);
                TempData["ErrorMessage"] = "Lỗi khi xuất báo cáo.";
                return RedirectToAction(nameof(ShiftDetails), new { shiftId });
            }
        }
    }
}

