using System.Text;
using System.Text.RegularExpressions;

namespace ColorPickerApp.Services
{
    public class ExportService
    {
        public byte[] ExportToHtml(string[] colors)
        {
            var htmlContent = new StringBuilder();
            htmlContent.AppendLine("<!DOCTYPE html>");
            htmlContent.AppendLine("<html lang=\"ru\">");
            htmlContent.AppendLine("<head>");
            htmlContent.AppendLine("<meta charset=\"UTF-8\">");
            htmlContent.AppendLine("<title>Цветовая палитра</title>");
            htmlContent.AppendLine("<style>");
            htmlContent.AppendLine("body { font-family: Arial, sans-serif; }");
            htmlContent.AppendLine("table { width: 100%; border-collapse: collapse; }");
            htmlContent.AppendLine("td { padding: 20px; text-align: center; color: white; text-shadow: 1px 1px 2px black; }");
            htmlContent.AppendLine("</style>");
            htmlContent.AppendLine("</head>");
            htmlContent.AppendLine("<body>");
            htmlContent.AppendLine("<h1>Цветовая палитра</h1>");
            htmlContent.AppendLine("<table>");
            foreach (var color in colors)
            {
                htmlContent.AppendLine($"<tr><td style=\"background-color: {color};\">{color}</td></tr>");
            }
            htmlContent.AppendLine("</table>");
            htmlContent.AppendLine("</body>");
            htmlContent.AppendLine("</html>");

            return Encoding.UTF8.GetBytes(htmlContent.ToString());
        }

        public byte[] ExportToAco(string[] colors)
        {
            using (var memoryStream = new MemoryStream())
            using (var writer = new BinaryWriter(memoryStream))
            {
            
                writer.Write((short)1); // Версия 1
                writer.Write((short)colors.Length); // Количество цветов

                // Запись цветов
                foreach (var color in colors)
                {
                    var rgb = HexToRgb(color);
                    writer.Write((short)0); // Тип цвета (0 — RGB)
                    writer.Write((short)(rgb[0] * 257)); // Красный (0-65535)
                    writer.Write((short)(rgb[1] * 257)); // Зелёный (0-65535)
                    writer.Write((short)(rgb[2] * 257)); // Синий (0-65535)
                    writer.Write((short)0); // Reserved (всегда 0)
                }

                return memoryStream.ToArray();
            }
        }

        // Экспорт в GPL (GIMP)
        public byte[] ExportToGpl(string[] colors)
        {
            var gplContent = new StringBuilder();
            gplContent.AppendLine("GIMP Palette");
            gplContent.AppendLine("Name: Exported Palette");
            gplContent.AppendLine("Columns: 4");
            gplContent.AppendLine("#");

            foreach (var color in colors)
            {
                var rgb = HexToRgb(color);
                gplContent.AppendLine($"{rgb[0]} {rgb[1]} {rgb[2]} Untitled");
            }

            return Encoding.UTF8.GetBytes(gplContent.ToString());
        }
        public List<string> ImportFromCss(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                var content = reader.ReadToEnd();

                // Регулярное выражение для поиска цветов в CSS:
                // 1. HEX форматы: #RGB, #RRGGBB, #RRGGBBAA
                // 2. RGB/RGBA функции: rgb(255, 255, 255), rgba(255, 255, 255, 0.5)
                // 3. HSL/HSLA функции: hsl(120, 100%, 50%), hsla(120, 100%, 50%, 0.5)
                // 4. Именованные цвета: red, blue и т.д.

                var colors = new List<string>();

                // 1. Ищем HEX цвета
                var hexMatches = Regex.Matches(content, @"#([A-Fa-f0-9]{8}|[A-Fa-f0-9]{6}|[A-Fa-f0-9]{4}|[A-Fa-f0-9]{3})\b");
                colors.AddRange(hexMatches.Cast<Match>().Select(m => m.Value));

                // 2. Ищем RGB/RGBA цвета
                var rgbMatches = Regex.Matches(content, @"rgba?\(\s*(\d{1,3}\s*,\s*\d{1,3}\s*,\s*\d{1,3}(?:\s*,\s*[\d\.]+)?\s*\)");
                foreach (Match match in rgbMatches)
                {
                    var rgbValues = Regex.Matches(match.Value, @"\d+");
                    if (rgbValues.Count >= 3)
                    {
                        byte r = byte.Parse(rgbValues[0].Value);
                        byte g = byte.Parse(rgbValues[1].Value);
                        byte b = byte.Parse(rgbValues[2].Value);
                        colors.Add($"#{r:X2}{g:X2}{b:X2}");
                    }
                }

                // 3. Ищем HSL/HSLA цвета (конвертируем в HEX)
                var hslMatches = Regex.Matches(content, @"hsla?\(\s*(\d{1,3}\s*,\s*\d{1,3}%\s*,\s*\d{1,3}%(?:\s*,\s*[\d\.]+)?\s*\)");
                foreach (Match match in hslMatches)
                {
                    var hslValues = Regex.Matches(match.Value, @"[\d\.]+");
                    if (hslValues.Count >= 3)
                    {
                        float h = float.Parse(hslValues[0].Value);
                        float s = float.Parse(hslValues[1].Value.Replace("%", "")) / 100f;
                        float l = float.Parse(hslValues[2].Value.Replace("%", "")) / 100f;

                        var hexColor = HslToHex(h, s, l);
                        colors.Add(hexColor);
                    }
                }

                // 4. Ищем именованные цвета
                var namedColors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"red", "#FF0000"}, {"green", "#008000"}, {"blue", "#0000FF"},
                {"yellow", "#FFFF00"}, {"black", "#000000"}, {"white", "#FFFFFF"},
                // Добавьте другие именованные цвета по необходимости
            };

                var namedMatches = Regex.Matches(content, @"\b(red|green|blue|yellow|black|white)\b", RegexOptions.IgnoreCase);
                colors.AddRange(namedMatches.Cast<Match>()
                    .Select(m => namedColors[m.Value.ToLower()]));

                return colors.Distinct().ToList();
            }
        }

        private string HslToHex(float h, float s, float l)
        {
            // Конвертация HSL в RGB, затем в HEX
            float c = (1 - Math.Abs(2 * l - 1)) * s;
            float x = c * (1 - Math.Abs((h / 60) % 2 - 1));
            float m = l - c / 2;

            float r = 0, g = 0, b = 0;

            if (h >= 0 && h < 60) { r = c; g = x; }
            else if (h >= 60 && h < 120) { r = x; g = c; }
            else if (h >= 120 && h < 180) { g = c; b = x; }
            else if (h >= 180 && h < 240) { g = x; b = c; }
            else if (h >= 240 && h < 300) { r = x; b = c; }
            else if (h >= 300 && h < 360) { r = c; b = x; }

            byte rByte = (byte)((r + m) * 255);
            byte gByte = (byte)((g + m) * 255);
            byte bByte = (byte)((b + m) * 255);

            return $"#{rByte:X2}{gByte:X2}{bByte:X2}";
        }
    
    private int[] HexToRgb(string hex)
        {
            return new int[]
            {
                Convert.ToInt32(hex.Substring(1, 2), 16),
                Convert.ToInt32(hex.Substring(3, 2), 16),
                Convert.ToInt32(hex.Substring(5, 2), 16)
            };
        }
    }
}
 
