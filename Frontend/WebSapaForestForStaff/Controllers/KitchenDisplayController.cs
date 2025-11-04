using Microsoft.AspNetCore.Mvc;

namespace WebSapaFoRestForStaff.Controllers
{
    public class KitchenDisplayController : Controller
    {
        /// <summary>
        /// Main KDS screen for Sous Chef
        /// GET: /KitchenDisplay
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Station screen (filtered by course type)
        /// GET: /KitchenDisplay/Station?courseType=MainCourse
        /// </summary>
        public IActionResult Station(string courseType)
        {
            ViewBag.CourseType = courseType;
            return View();
        }
    }
}