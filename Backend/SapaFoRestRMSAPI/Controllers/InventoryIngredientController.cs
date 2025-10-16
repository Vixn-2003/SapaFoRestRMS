using BusinessAccessLayer.DTOs.Inventory;
using BusinessAccessLayer.DTOs.Manager;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryIngredientController : Controller
    {

        private readonly IInventoryIngredientService _inventoryIngredientService;

        public InventoryIngredientController(IInventoryIngredientService inventoryIngredientService)
        {
            _inventoryIngredientService = inventoryIngredientService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryIngredientWithBatchDTO>>> GetManagerCombo()
        {
            try
            {
                // Get list ingredient
                var ingredient = await _inventoryIngredientService.GetAllIngredient();
                if (!ingredient.Any())
                {
                    //Can't find ingredient
                    return NotFound("No ingredient found");
                }
                // Find list ingredient
                return Ok(ingredient);
            }
            //Error
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        //// GET: InventoryIngredientController
        //public ActionResult Index()
        //{
        //    return View();
        //}

        //// GET: InventoryIngredientController/Details/5
        //public ActionResult Details(int id)
        //{
        //    return View();
        //}

        //// GET: InventoryIngredientController/Create
        //public ActionResult Create()
        //{
        //    return View();
        //}

        //// POST: InventoryIngredientController/Create
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Create(IFormCollection collection)
        //{
        //    try
        //    {
        //        return RedirectToAction(nameof(Index));
        //    }
        //    catch
        //    {
        //        return View();
        //    }
        //}

        //// GET: InventoryIngredientController/Edit/5
        //public ActionResult Edit(int id)
        //{
        //    return View();
        //}

        //// POST: InventoryIngredientController/Edit/5
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Edit(int id, IFormCollection collection)
        //{
        //    try
        //    {
        //        return RedirectToAction(nameof(Index));
        //    }
        //    catch
        //    {
        //        return View();
        //    }
        //}

        //// GET: InventoryIngredientController/Delete/5
        //public ActionResult Delete(int id)
        //{
        //    return View();
        //}

        //// POST: InventoryIngredientController/Delete/5
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Delete(int id, IFormCollection collection)
        //{
        //    try
        //    {
        //        return RedirectToAction(nameof(Index));
        //    }
        //    catch
        //    {
        //        return View();
        //    }
        //}
    }
}
