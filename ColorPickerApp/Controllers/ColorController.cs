using Microsoft.AspNetCore.Mvc;
using ColorPickerApp.Services; // Убедитесь, что это пространство имен вашего ColorService и ExportService
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Net;

namespace ColorPickerApp.Controllers
{
    public class ColorController : Controller
    {
        private readonly ExportService _colorExportService;
        // PaletteGeneratorService больше не нужен, так как ColorService статический

        public ColorController(ExportService colorExportService) // Внедряем только ExportService
        {
            _colorExportService = colorExportService;
        }

        public IActionResult Index(string baseColor = "#3498db")
        {
            string validatedBaseColor = baseColor;
            if (string.IsNullOrEmpty(validatedBaseColor) || !IsValidHex(validatedBaseColor))
            {
                validatedBaseColor = "#3498db"; // Цвет по умолчанию, если передан неверный
            }
            // Убедимся, что HEX всегда с # для ViewBag и для сервиса
            if (!validatedBaseColor.StartsWith("#"))
            {
                validatedBaseColor = "#" + validatedBaseColor;
            }
            ViewBag.BaseColor = validatedBaseColor; // Передаем в ViewBag для использования в input

            // Генерируем простую палитру для первоначального отображения
            // JavaScript затем загрузит полный набор через API
            // Используем статический метод из ColorService
            List<string> initialPalette = ColorService.GenerateSimplePalette(validatedBaseColor);
            return View(initialPalette);
        }

        // API endpoint для генерации палитр через AJAX
        [HttpGet("api/palette/generate")] // Маршрут для API
        [Produces("application/json")]   // Указываем, что возвращаем JSON
        public ActionResult<Dictionary<string, List<string>>> GeneratePalettesApi([FromQuery] string baseColor)
        {
            string decodedBaseColor = baseColor;
            if (!string.IsNullOrEmpty(baseColor))
            {
                if (baseColor.Contains("%"))
                {
                    decodedBaseColor = WebUtility.UrlDecode(baseColor);
                }
            }

            if (string.IsNullOrEmpty(decodedBaseColor) || !IsValidHex(decodedBaseColor))
            {
                return BadRequest(new { message = "Неверный или отсутствующий параметр baseColor. Ожидается HEX формат (например, #RRGGBB или RRGGBB)." });
            }

            // Убедимся, что HEX всегда с # для сервиса
            if (!decodedBaseColor.StartsWith("#"))
            {
                decodedBaseColor = "#" + decodedBaseColor;
            }

            // Используем статический метод из ColorService
            var palettes = ColorService.GenerateAllHarmonies(decodedBaseColor);
            if (palettes.ContainsKey("error"))
            {
                // Отправляем ошибку, если сервис ее вернул
                return BadRequest(new { message = palettes["error"].FirstOrDefault() ?? "Неизвестная ошибка генерации палитры." });
            }
            return Ok(palettes);
        }

        private bool IsValidHex(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return false;

            return Regex.IsMatch(hex, @"^#?([A-Fa-f0-9]{8}|[A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$");
        }
        public IActionResult ExportToHtml([FromQuery] string colors)
        {
            if (string.IsNullOrEmpty(colors)) return BadRequest("Нет цветов для экспорта.");
            // Декодируем каждый цвет и добавляем #, если отсутствует
            var colorArray = colors.Split(',')
                                   .Select(c => WebUtility.UrlDecode(c)) // Декодируем каждый цвет
                                   .Select(c => c.Trim())
                                   .Select(c => c.StartsWith("#") ? c : "#" + c)
                                   .ToArray();
            var fileContent = _colorExportService.ExportToHtml(colorArray);
            return File(fileContent, "text/html", "palette.html");
        }

        public IActionResult ExportToAco([FromQuery] string colors)
        {
            if (string.IsNullOrEmpty(colors)) return BadRequest("Нет цветов для экспорта.");
            var colorArray = colors.Split(',')
                                   .Select(c => WebUtility.UrlDecode(c))
                                   .Select(c => c.Trim())
                                   .Select(c => c.StartsWith("#") ? c : "#" + c)
                                   .ToArray();
            var fileContent = _colorExportService.ExportToAco(colorArray);
            return File(fileContent, "application/octet-stream", "palette.aco");
        }

        public IActionResult ExportToGpl([FromQuery] string colors)
        {
            if (string.IsNullOrEmpty(colors)) return BadRequest("Нет цветов для экспорта.");
            var colorArray = colors.Split(',')
                                   .Select(c => WebUtility.UrlDecode(c))
                                   .Select(c => c.Trim())
                                   .Select(c => c.StartsWith("#") ? c : "#" + c)
                                   .ToArray();
            var fileContent = _colorExportService.ExportToGpl(colorArray);
            return File(fileContent, "text/plain", "palette.gpl");
        }
    }
}

