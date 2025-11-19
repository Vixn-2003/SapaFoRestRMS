using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace SapaFoRestRMSAPI.Controllers
{
    /// <summary>
    /// Controller quản lý Roles
    /// Chỉ Admin có quyền quản lý roles
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RolesController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        /// <summary>
        /// Admin: Lấy danh sách tất cả roles
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct = default)
        {
            var roles = await _roleService.GetAllAsync(ct);
            return Ok(roles);
        }

        /// <summary>
        /// Admin: Lấy chi tiết role
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id, CancellationToken ct = default)
        {
            var role = await _roleService.GetByIdAsync(id, ct);
            if (role == null)
            {
                return NotFound();
            }
            return Ok(role);
        }
    }
}

