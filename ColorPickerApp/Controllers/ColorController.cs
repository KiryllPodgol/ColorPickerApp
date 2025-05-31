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
       
        private readonly ImportService _importService;
        public ColorController(ExportService colorExportService, ImportService importService)
        {
            _colorExportService = colorExportService;
            _importService = importService;
        }

        public IActionResult Index(string baseColor = "#3498db")
        {
            string validatedBaseColor = baseColor;
            if (string.IsNullOrEmpty(validatedBaseColor) || !IsValidHex(validatedBaseColor))
            {
                validatedBaseColor = "#3498db";
            }
            
            if (!validatedBaseColor.StartsWith("#"))
            {
                validatedBaseColor = "#" + validatedBaseColor;
            }
            ViewBag.BaseColor = validatedBaseColor; 
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

            
            if (!decodedBaseColor.StartsWith("#"))
            {
                decodedBaseColor = "#" + decodedBaseColor;
            }

          
            var palettes = ColorService.GenerateAllHarmonies(decodedBaseColor);
            if (palettes.ContainsKey("error"))
            {
                
                return BadRequest(new { message = palettes["error"].FirstOrDefault() ?? "Неизвестная ошибка генерации палитры." });
            }
            return Ok(palettes);
        }
        [HttpGet("api/palette/independent")]
        [Produces("application/json")]
        public ActionResult<List<string>> GenerateIndependentPaletteApi()
        {
            var palette = ColorService.GenerateIndependentPalette();
            return Ok(palette);
        }
        private bool IsValidHex(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return false;

            return Regex.IsMatch(hex, @"^#?([A-Fa-f0-9]{8}|[A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$");
        }
        public IActionResult ExportToHtml([FromQuery] string colors)
        {
            if (string.IsNullOrEmpty(colors)) return BadRequest("Нет цветов для экспорта.");
            
            var colorArray = colors.Split(',')
                                   .Select(c => WebUtility.UrlDecode(c)) 
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

        [HttpPost("api/palette/import")]
        [Produces("application/json")]
        public async Task<ActionResult<List<string>>> ImportPalette(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Файл не выбран или пуст." });
            }

            if (file.Length > 5 * 1024 * 1024) // 5MB limit
            {
                return BadRequest(new { message = "Файл слишком большой. Максимальный размер - 5MB." });
            }

            var fileExtension = Path.GetExtension(file.FileName?.ToLowerInvariant() ?? "");
            if (string.IsNullOrEmpty(fileExtension))
            {
                return BadRequest(new { message = "Не удалось определить тип файла." });
            }

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;

                    List<string> colors;
                    switch (fileExtension)
                    {
                        case ".html":
                        case ".htm":
                            colors = _importService.ImportFromHtml(stream);
                            break;
                        case ".gpl":
                            colors = _importService.ImportFromGimp(stream);
                            break;
                        case ".aco":
                            colors = _importService.ImportFromAco(stream);
                            break;
                        case ".css":
                            colors = _importService.ImportFromCss(stream);
                            break;
                        default:
                            return BadRequest(new { message = $"Формат {fileExtension} не поддерживается для импорта." });
                    }

                    if (colors == null || colors.Count == 0)
                    {
                        return NotFound(new { message = "Не удалось извлечь цвета из файла или файл не содержит поддерживаемых цветов." });
                    }

                    var formattedColors = colors
                        .Select(c => c.Trim())
                        .Where(c => !string.IsNullOrEmpty(c))
                        .Select(c => c.StartsWith("#") ? c : "#" + c)
                        .Where(IsValidHex)
                        .Distinct()
                        .ToList();

                    if (formattedColors.Count == 0)
                    {
                        return NotFound(new { message = "Извлеченные данные не содержат допустимых HEX цветов." });
                    }

                    return Ok(formattedColors);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Ошибка при обработке файла: {ex.Message}" });
            }
        }

    }
  
}

