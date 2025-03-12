using Microsoft.AspNetCore.Mvc;
using ColorPickerApp.Services;
using System.Collections.Generic;

namespace ColorPickerApp.Controllers
{
    public class ColorController : Controller
    {
        private readonly ExportService _colorExportService;
        public ColorController(ExportService colorExportService)
        {
            _colorExportService = colorExportService;
        }

        public IActionResult Index(string baseColor = "#3498db") // По умолчанию синий
        {
            if (!baseColor.StartsWith("#"))
            {
                baseColor = "#" + baseColor;
            }

            List<string> palette = ColorService.GeneratePalette(baseColor);
            return View(palette);
        }
        public IActionResult ExportToHtml(string colors)
        {
            var colorArray = colors.Split(','); // Разделяем строку на массив
            var fileContent = _colorExportService.ExportToHtml(colorArray);
            return File(fileContent, "text/html", "palette.html");
        }

        public IActionResult ExportToAco(string colors)
        {
            var colorArray = colors.Split(','); // Разделяем строку на массив
            var fileContent = _colorExportService.ExportToAco(colorArray);
            return File(fileContent, "application/octet-stream", "palette.aco");
        }

        public IActionResult ExportToGpl(string colors)
        {
            var colorArray = colors.Split(','); // Разделяем строку на массив
            var fileContent = _colorExportService.ExportToGpl(colorArray);
            return File(fileContent, "text/plain", "palette.gpl");
        }
    }
}
