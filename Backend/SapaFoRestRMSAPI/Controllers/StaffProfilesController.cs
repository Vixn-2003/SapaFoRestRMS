using System.Threading;
using System.Threading.Tasks;
using BusinessAccessLayer.DTOs.UserManagement;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize(Roles = "Admin,Manager")]
	public class StaffProfilesController : ControllerBase
	{
		private readonly IStaffProfileService _service;

		public StaffProfilesController(IStaffProfileService service)
		{
			_service = service;
		}

		[HttpGet]
		public async Task<IActionResult> GetAll(CancellationToken ct)
		{
			var result = await _service.GetAllAsync(ct);
			return Ok(result);
		}

		[HttpGet("{userId:int}")]
		public async Task<IActionResult> Get(int userId, CancellationToken ct)
		{
			var dto = await _service.GetAsync(userId, ct);
			if (dto == null) return NotFound();
			return Ok(dto);
		}

		[HttpPut("{userId:int}")]
		public async Task<IActionResult> Update(int userId, [FromBody] StaffProfileUpdateDto update, CancellationToken ct)
		{
			await _service.UpdateAsync(userId, update, ct);
			return NoContent();
		}
	}
}
