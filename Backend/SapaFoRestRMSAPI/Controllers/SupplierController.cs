// File: SapaFoRestRMSAPI.Controllers/SupplierController.cs (Cập Nhật)

using BusinessAccessLayer.DTOs.Inventory;
using BusinessAccessLayer.DTOs.Manager;
using BusinessAccessLayer.Services.Interfaces; // Giữ tên Interface cũ
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SapaFoRestRMSAPI.Controllers
{
    [ApiController]
    [Route("api/inventory/[controller]")] // Đổi route gốc để dễ quản lý module Inventory
    public class SupplierController : ControllerBase
    {
        private readonly IManagerSupplierService _managerSupplier; 
        private readonly ISupplierManagerService _supplierManager;

        public SupplierController(IManagerSupplierService managerSupplier, ISupplierManagerService supplierManager)
        {
            _managerSupplier = managerSupplier;
            _supplierManager = supplierManager;
        }

        // --- API CŨ: Lấy tất cả nhà cung cấp (Dùng cho mục đích cơ bản) ---
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SupplierDTO>>> Index()
        {
            try
            {
                var supplier = await _managerSupplier.GetManagerAllSupplier();
                if (!supplier.Any())
                {
                    return NotFound("No supplier found");
                }
                return Ok(supplier);
            }
            catch (Exception ex)
            {
                // Log exception
                return StatusCode(500, "An error occurred while getting the supplier");
            }
        }

        // --- API MỚI 1: DANH SÁCH & TỔNG HỢP (List View) ---
        /// <summary>
        /// Lấy danh sách nhà cung cấp kèm thống kê tổng hợp (Total Orders, Total Value, Last Order).
        /// Route: GET api/inventory/supplier/summary-list
        /// </summary>
        [HttpGet("summary-list")]
        public async Task<ActionResult<IEnumerable<SupplierListDto>>> GetSuppliersSummaryList()
        {
            try
            {
                var suppliers = await _supplierManager.GetSuppliersSummaryAsync();
                if (!suppliers.Any())
                {
                    return NotFound("No supplier summary data found.");
                }
                return Ok(suppliers);
            }
            catch (Exception ex)
            {
                // Log exception
                return StatusCode(500, "An error occurred while getting supplier summary data.");
            }
        }

        // --- API MỚI 2: TOP SUPPLIERS (Dashboard) ---
        /// <summary>
        /// Lấy top nhà cung cấp theo Giá trị đơn hàng trong một khoảng thời gian.
        /// Route: GET api/inventory/supplier/top-by-value?startDate=...&endDate=...
        /// </summary>
        [HttpGet("top-by-value")]
        public async Task<ActionResult<IEnumerable<TopSupplierDto>>> GetTopSuppliers(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            if (startDate == default || endDate == default)
            {
                return BadRequest("Phải cung cấp startDate và endDate hợp lệ.");
            }

            var topSuppliers = await _supplierManager.GetTopSuppliersAsync(startDate, endDate);
            return Ok(topSuppliers);
        }

        // --- API MỚI 3: LỊCH SỬ ĐƠN HÀNG (Detail Tab: Orders) ---
        /// <summary>
        /// Lấy lịch sử đơn hàng của nhà cung cấp theo Supplier ID (Detail Tab: Orders History)
        /// Route: GET api/inventory/supplier/{id}/orders-history
        /// </summary>
        [HttpGet("{id}/orders-history")]
        public async Task<ActionResult<IEnumerable<OrderHistoryDto>>> GetSupplierOrdersHistory(int id)
        {
            try
            {
                var orders = await _supplierManager.GetHistoryAsync(id);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                // Log exception
                return StatusCode(500, "An error occurred while fetching order history.");
            }
        }

        // --- API MỚI 4: DANH MỤC NGUYÊN LIỆU (Detail Tab: Products) ---
        /// <summary>
        /// Lấy danh mục nguyên liệu/sản phẩm nhà cung cấp cung cấp (Detail Tab: Products)
        /// Route: GET api/inventory/supplier/{id}/products-supplied
        /// </summary>
        [HttpGet("{id}/products-supplied")]
        public async Task<ActionResult<IEnumerable<SupplierIngredientDto>>> GetSupplierProductsSupplied(int id)
        {
            try
            {
                var products = await _supplierManager.GetProductsAsync(id);
                return Ok(products);
            }
            catch (Exception ex)
            {
                // Log exception
                return StatusCode(500, "An error occurred while fetching supplier products.");
            }
        }
    }
}