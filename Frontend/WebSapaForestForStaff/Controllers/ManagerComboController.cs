using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebSapaForestForStaff.Controllers
{
    public class ManagerComboController : Controller
    {

        private readonly HttpClient _httpClient;

        public ManagerComboController(HttpClient httpClient)
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://localhost:7096/");
        }
        // GET: ComboController
        public ActionResult Index()
        {
            return View();
        }

        // GET: ComboController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: ComboController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ComboController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: ComboController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: ComboController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: ComboController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: ComboController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
