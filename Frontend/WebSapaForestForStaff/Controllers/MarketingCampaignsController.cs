using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Web;
using WebSapaFoRestRMSForStaff.Models.CampaignDTO;

namespace WebSapaFoRestRMSForStaff.Controllers
{
    public class MarketingCampaignsController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl = "https://localhost:7096/api/MarketingCampaigns";

        public MarketingCampaignsController()
        {
            _httpClient = new HttpClient();
        }

        // GET: /MarketingCampaigns
        public async Task<IActionResult> Index(
            string searchTerm = "",
            string campaignType = "",
            string status = "",
            string startDate = "",
            string endDate = "",
            int pageNumber = 1,
            int pageSize = 10)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);

            if (!string.IsNullOrEmpty(searchTerm)) query["searchTerm"] = searchTerm;
            if (!string.IsNullOrEmpty(campaignType)) query["campaignType"] = campaignType;
            if (!string.IsNullOrEmpty(status)) query["status"] = status;
            if (!string.IsNullOrEmpty(startDate)) query["startDate"] = startDate;
            if (!string.IsNullOrEmpty(endDate)) query["endDate"] = endDate;

            query["pageNumber"] = pageNumber.ToString();
            query["pageSize"] = pageSize.ToString();

            string apiUrl = $"{_apiBaseUrl}/list?{query}";

            try
            {
                var response = await _httpClient.GetAsync(apiUrl);

                if (!response.IsSuccessStatusCode)
                    return View("Error");

                var jsonString = await response.Content.ReadAsStringAsync();
                var pagedResult = JsonConvert.DeserializeObject<PagedResult<MarketingCampaignDto>>(jsonString);

                return View(pagedResult);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi tải dữ liệu: {ex.Message}";
                return View("Error");
            }
        }

        // GET: /MarketingCampaigns/Details/5
        public async Task<IActionResult> Details(int id, int pageNumber = 1)
        {
            var apiUrl = $"{_apiBaseUrl}/{id}";

            try
            {
                var campaign = await _httpClient.GetFromJsonAsync<MarketingCampaignDto>(apiUrl);

                if (campaign == null)
                    return NotFound();

                ViewData["PageNumber"] = pageNumber;
                return View(campaign);
            }
            catch (HttpRequestException ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi gọi API: {ex.Message}";
                return StatusCode(500);
            }
        }

        // GET: /MarketingCampaigns/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /MarketingCampaigns/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MarketingCampaignCreateDto dto, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
                return View(dto);

            try
            {
                using var formData = new MultipartFormDataContent();

                // Add all properties
                formData.Add(new StringContent(dto.Title), "Title");
                if (dto.StartDate.HasValue) formData.Add(new StringContent(dto.StartDate.Value.ToString("yyyy-MM-dd")), "StartDate");
                if (dto.EndDate.HasValue) formData.Add(new StringContent(dto.EndDate.Value.ToString("yyyy-MM-dd")), "EndDate");
                if (!string.IsNullOrEmpty(dto.Status)) formData.Add(new StringContent(dto.Status), "Status");
                if (dto.VoucherId.HasValue) formData.Add(new StringContent(dto.VoucherId.Value.ToString()), "VoucherId");
                if (dto.Budget.HasValue) formData.Add(new StringContent(dto.Budget.Value.ToString()), "Budget");
                if (!string.IsNullOrEmpty(dto.CampaignType)) formData.Add(new StringContent(dto.CampaignType), "CampaignType");
                if (!string.IsNullOrEmpty(dto.TargetAudience)) formData.Add(new StringContent(dto.TargetAudience), "TargetAudience");
                if (dto.TargetReach.HasValue) formData.Add(new StringContent(dto.TargetReach.Value.ToString()), "TargetReach");
                if (dto.TargetRevenue.HasValue) formData.Add(new StringContent(dto.TargetRevenue.Value.ToString()), "TargetRevenue");

                // Add image file if provided
                if (imageFile != null && imageFile.Length > 0)
                {
                    var fileContent = new StreamContent(imageFile.OpenReadStream());
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(imageFile.ContentType);
                    formData.Add(fileContent, "ImageFile", imageFile.FileName);
                }

                var response = await _httpClient.PostAsync(_apiBaseUrl, formData);

                if (response.IsSuccessStatusCode)
                {
                    var createdCampaign = await response.Content.ReadFromJsonAsync<MarketingCampaignDto>();
                    TempData["SuccessMessage"] = "Tạo chiến dịch thành công!";
                    return RedirectToAction("Details", new { id = createdCampaign?.CampaignId });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", $"Tạo chiến dịch thất bại: {errorContent}");
                    return View(dto);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Có lỗi xảy ra: {ex.Message}");
                return View(dto);
            }
        }

        // GET: /MarketingCampaigns/Edit/5
        public async Task<IActionResult> Edit(int id, int pageNumber = 1)
        {
            var apiUrl = $"{_apiBaseUrl}/{id}";

            try
            {
                var campaign = await _httpClient.GetFromJsonAsync<MarketingCampaignDto>(apiUrl);

                if (campaign == null)
                    return NotFound();

                ViewData["PageNumber"] = pageNumber;
                return View(campaign);
            }
            catch (HttpRequestException ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi gọi API: {ex.Message}";
                return StatusCode(500);
            }
        }

        // POST: /MarketingCampaigns/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MarketingCampaignDto model, IFormFile? imageFile, int pageNumber = 1)
        {
            if (!ModelState.IsValid)
            {
                ViewData["PageNumber"] = pageNumber;
                return View(model);
            }

            try
            {
                using var formData = new MultipartFormDataContent();

                // Add properties
                if (!string.IsNullOrEmpty(model.Title)) formData.Add(new StringContent(model.Title), "Title");
                if (model.StartDate.HasValue) formData.Add(new StringContent(model.StartDate.Value.ToString("yyyy-MM-dd")), "StartDate");
                if (model.EndDate.HasValue) formData.Add(new StringContent(model.EndDate.Value.ToString("yyyy-MM-dd")), "EndDate");
                if (!string.IsNullOrEmpty(model.Status)) formData.Add(new StringContent(model.Status), "Status");
                if (model.VoucherId.HasValue) formData.Add(new StringContent(model.VoucherId.Value.ToString()), "VoucherId");
                if (model.Budget.HasValue) formData.Add(new StringContent(model.Budget.Value.ToString()), "Budget");
                if (!string.IsNullOrEmpty(model.CampaignType)) formData.Add(new StringContent(model.CampaignType), "CampaignType");
                if (!string.IsNullOrEmpty(model.TargetAudience)) formData.Add(new StringContent(model.TargetAudience), "TargetAudience");
                if (model.ViewCount.HasValue) formData.Add(new StringContent(model.ViewCount.Value.ToString()), "ViewCount");
                if (model.RevenueGenerated.HasValue) formData.Add(new StringContent(model.RevenueGenerated.Value.ToString()), "RevenueGenerated");
                if (model.TargetReach.HasValue) formData.Add(new StringContent(model.TargetReach.Value.ToString()), "TargetReach");
                if (model.TargetRevenue.HasValue) formData.Add(new StringContent(model.TargetRevenue.Value.ToString()), "TargetRevenue");

                // Add image if provided
                if (imageFile != null && imageFile.Length > 0)
                {
                    var fileContent = new StreamContent(imageFile.OpenReadStream());
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(imageFile.ContentType);
                    formData.Add(fileContent, "ImageFile", imageFile.FileName);
                }

                var response = await _httpClient.PutAsync($"{_apiBaseUrl}/{id}", formData);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Cập nhật chiến dịch thành công!";
                    return RedirectToAction("Index", new { pageNumber });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", $"Cập nhật thất bại: {errorContent}");
                    ViewData["PageNumber"] = pageNumber;
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Có lỗi xảy ra: {ex.Message}");
                ViewData["PageNumber"] = pageNumber;
                return View(model);
            }
        }

        // DELETE: /MarketingCampaigns/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id, int pageNumber = 1)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/{id}");

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Xóa chiến dịch thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Xóa thất bại. Status: {response.StatusCode}";
                }

                return RedirectToAction("Index", new { pageNumber });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Đã xảy ra lỗi: {ex.Message}";
                return RedirectToAction("Index", new { pageNumber });
            }
        }
    }
}