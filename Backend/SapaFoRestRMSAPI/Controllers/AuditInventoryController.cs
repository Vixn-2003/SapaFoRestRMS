using BusinessAccessLayer.DTOs.Inventory;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Dbcontext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace SapaFoRestRMSAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuditInventoryController : ControllerBase
    {
        private readonly IAuditService _auditInventoryService;
        private readonly ILogger<AuditInventoryController> _logger;

        public AuditInventoryController(
            IAuditService auditInventoryService,
            ILogger<AuditInventoryController> logger)
        {
            _auditInventoryService = auditInventoryService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy tất cả đơn kiểm kê
        /// </summary>
        [HttpGet("GetAll")]
        public async Task<ActionResult<IEnumerable<AuditInventoryResponseDTO>>> GetAll()
        {
            try
            {
                var audits = await _auditInventoryService.GetAllAuditsAsync();
                return Ok(audits);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all audits");
                return StatusCode(500, new { message = "Có lỗi xảy ra khi lấy danh sách đơn kiểm kê" });
            }
        }

        /// <summary>
        /// Lấy chi tiết đơn kiểm kê theo ID
        /// </summary>
        [HttpGet("GetById/{id}")]
        public async Task<ActionResult<AuditInventoryResponseDTO>> GetById(string id)
        {
            try
            {
                var audit = await _auditInventoryService.GetAuditByIdAsync(id);

                if (audit == null)
                {
                    return NotFound(new { message = "Không tìm thấy đơn kiểm kê" });
                }

                return Ok(audit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit by id {AuditId}", id);
                return StatusCode(500, new { message = "Có lỗi xảy ra khi lấy thông tin đơn kiểm kê" });
            }
        }

        /// <summary>
        /// Tạo đơn kiểm kê mới
        /// </summary>
        //[HttpPost("Create")]
        //public async Task<ActionResult> CreateAudit([FromForm] CreateAuditInventoryDTO request)
        //{
        //    try
        //    {
        //        var result = await _auditInventoryService.CreateAuditAsync(request);

        //        if (result.Success)
        //        {
        //            return Ok(new { success = true, message = result.Message, data = result.Data });
        //        }

        //        return BadRequest(new { success = false, message = result.Message });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error creating audit");
        //        return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi tạo đơn kiểm kê" });
        //    }
        //}

        /// <summary>
        /// Xác nhận đơn kiểm kê
        /// </summary>
        [HttpPost("Confirm/{id}")]
        public async Task<ActionResult> ConfirmAudit(string id, [FromBody] ConfirmAuditInventoryDTO request)
        {
            try
            {
                var audit = await _auditInventoryService.GetAuditByIdAsync(id);

                if (audit == null)
                {
                    return NotFound(new { message = "Không tìm thấy đơn kiểm kê" });
                }

                audit.AuditStatus = request.AuditStatus;
                audit.ConfirmerId = request.ConfirmerId;
                audit.ConfirmerPhone = request.ConfirmerPhone;
                audit.ConfirmerName = request.ConfirmerName;
                audit.ConfirmerPosition = request.ConfirmerPosition;
                audit.ConfirmedAt = request.ConfirmedAt;

                var result = await _auditInventoryService.ConfirmAuditAsync(id, audit);

                if (result)
                {
                    return Ok(new { success = true, message = "Thao tác thành công" });
                }

                return BadRequest(new { success = false, message = "Xảy ra lỗi trong quá trình xử lý" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming audit {AuditId}", id);
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi xác nhận đơn kiểm kê" });
            }
        }

        /// <summary>
        /// Xóa đơn kiểm kê
        /// </summary>
        //[HttpDelete("Delete/{id}")]
        //public async Task<ActionResult> DeleteAudit(int id)
        //{
        //    try
        //    {
        //        var result = await _auditInventoryService.DeleteAuditAsync(id);

        //        if (result.Success)
        //        {
        //            return Ok(new { success = true, message = result.Message });
        //        }

        //        return BadRequest(new { success = false, message = result.Message });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error deleting audit {AuditId}", id);
        //        return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi xóa đơn kiểm kê" });
        //    }
        //}
    }
}
