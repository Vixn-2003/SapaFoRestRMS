using Microsoft.AspNetCore.Mvc;
using WebSapaForestForStaff.Models;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace WebSapaForestForStaff.Controllers
{
    public class BrandBannerController : Controller
    {
        private readonly HttpClient _client;
        private readonly string _apiUrl = "https://localhost:7096/api/BrandBanner";

        public BrandBannerController(IHttpClientFactory httpClientFactory)
        {
            _client = httpClientFactory.CreateClient();
        }

        public async Task<IActionResult> Index(string? status = null, string? title = null, int pageNumber = 1)
        {
            string apiUrl = $"{_apiUrl}/filter?status={status}&title={title}&pageNumber={pageNumber}&pageSize=7";

            var response = await _client.GetAsync(apiUrl);
            var banners = new List<BrandBannerViewModel>();
            int totalPages = 1;
            int totalItems = 0;

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject(json);
                totalPages = result.totalPages;
                totalItems = result.totalItems;
                banners = JsonConvert.DeserializeObject<List<BrandBannerViewModel>>(result.data.ToString());
            }

            var statuses = await GetStatusesAsync();

            var viewModel = new BannerListViewModel
            {
                Banners = banners,
                Statuses = statuses,
                CurrentStatus = status,
                CurrentTitle = title,
                PageNumber = pageNumber,
                TotalPages = totalPages,
                TotalItems = totalItems
            };

            return View(viewModel);
        }

        // ----------------- ADD -----------------
        [HttpGet]
        public async Task<IActionResult> Add()
        {
            ViewBag.Statuses = await GetStatusesAsync();
            return View(new BrandBannerViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Add(BrandBannerViewModel model)
        {
            ViewBag.Statuses = await GetStatusesAsync();

            if (!ModelState.IsValid)
                return View(model);

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(model.Title ?? ""), "Title");
            content.Add(new StringContent(model.Status ?? ""), "Status");
            content.Add(new StringContent(model.StartDate?.ToString("yyyy-MM-dd") ?? ""), "StartDate");
            content.Add(new StringContent(model.EndDate?.ToString("yyyy-MM-dd") ?? ""), "EndDate");

            if (model.ImageFile != null)
            {
                var streamContent = new StreamContent(model.ImageFile.OpenReadStream());
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(model.ImageFile.ContentType);
                content.Add(streamContent, "ImageFile", model.ImageFile.FileName);
            }

            var response = await _client.PostAsync(_apiUrl, content);
            if (response.IsSuccessStatusCode)
                return RedirectToAction(nameof(Index));

            ModelState.AddModelError("", "Không thể thêm banner.");
            return View(model);
        }

        // ----------------- EDIT -----------------
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var response = await _client.GetAsync($"{_apiUrl}/{id}");
            if (!response.IsSuccessStatusCode)
                return NotFound();

            var json = await response.Content.ReadAsStringAsync();
            var banner = JsonConvert.DeserializeObject<BrandBannerViewModel>(json);

            ViewBag.Statuses = await GetStatusesAsync();
            return View("Add", banner);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, BrandBannerViewModel model)
        {
            ViewBag.Statuses = await GetStatusesAsync();

            if (!ModelState.IsValid)
                return View("Add", model);

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(id.ToString()), "BannerId");
            content.Add(new StringContent(model.Title ?? ""), "Title");
            content.Add(new StringContent(model.Status ?? ""), "Status");
            content.Add(new StringContent(model.StartDate?.ToString("yyyy-MM-dd") ?? ""), "StartDate");
            content.Add(new StringContent(model.EndDate?.ToString("yyyy-MM-dd") ?? ""), "EndDate");

            
            if (model.ImageFile != null)
            {
                var streamContent = new StreamContent(model.ImageFile.OpenReadStream());
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(model.ImageFile.ContentType);
                content.Add(streamContent, "ImageFile", model.ImageFile.FileName);
            }
            else if (!string.IsNullOrEmpty(model.ImageUrl))
            {
                content.Add(new StringContent(model.ImageUrl), "ImageUrl");
            }

            var response = await _client.PutAsync($"{_apiUrl}/{id}", content);
            if (response.IsSuccessStatusCode)
                return RedirectToAction(nameof(Index));

            ModelState.AddModelError("", "Không thể cập nhật banner.");
            return View("Add", model);
        }

        // ----------------- DELETE -----------------
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _client.DeleteAsync($"{_apiUrl}/{id}");
            return RedirectToAction(nameof(Index));
        }

        // ----------------- GET STATUSES -----------------
        private async Task<List<string>> GetStatusesAsync()
        {
            var statuses = new List<string>();
            var response = await _client.GetAsync($"{_apiUrl}/statuses");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                statuses = JsonConvert.DeserializeObject<List<string>>(json);
            }
            return statuses;
        }
    }
}
