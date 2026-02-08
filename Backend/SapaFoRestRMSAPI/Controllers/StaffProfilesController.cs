using System.Threading;
using System.Threading.Tasks;
using BusinessAccessLayer.DTOs.UserManagement;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{
	/// <summary>
	/// Controller quản lý Staff Profiles
	/// Admin: Quản lý staff profiles
	/// Owner: Xem staff profiles (để giám sát hiệu suất nhân viên)
	/// </summary>
	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public class StaffProfilesController : ControllerBase
	{
		private readonly IStaffProfileService _service;

		public StaffProfilesController(IStaffProfileService service)
		{
			_service = service;
		}

		/// <summary>
		/// Admin/Owner: Xem danh sách staff profiles
		/// </summary>
		[HttpGet]
		[Authorize(Roles = "Admin,Owner")]
		public async Task<IActionResult> GetAll(CancellationToken ct)
		{
			var result = await _service.GetAllAsync(ct);
			return Ok(result);
		}

		/// <summary>
		/// Admin/Owner: Xem chi tiết staff profile
		/// </summary>
		[HttpGet("{userId:int}")]
		[Authorize(Roles = "Admin,Owner")]
		public async Task<IActionResult> Get(int userId, CancellationToken ct)
		{
			var dto = await _service.GetAsync(userId, ct);
			if (dto == null) return NotFound();
			return Ok(dto);
		}

		/// <summary>
		/// Admin: Cập nhật staff profile
		/// </summary>
		[HttpPut("{userId:int}")]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> Update(int userId, [FromBody] StaffProfileUpdateDto update, CancellationToken ct)
		{
			await _service.UpdateAsync(userId, update, ct);
			return NoContent();
		}
	}
}
