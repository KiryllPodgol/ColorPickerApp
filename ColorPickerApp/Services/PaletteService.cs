using Microsoft.AspNetCore.Mvc;

namespace ColorPickerApp.Services
{
    public class PaletteService : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
