using System.Text;

namespace ColorPickerApp.Services
{
    public class ExportService
    {
        public byte[] ExportToHtml(string[] colors)
        {
            var htmlContent = new StringBuilder();
            htmlContent.AppendLine("<!DOCTYPE html>");
            htmlContent.AppendLine("<html lang=\"en\">");
            htmlContent.AppendLine("<head>");
            htmlContent.AppendLine("<meta charset=\"UTF-8\">");
            htmlContent.AppendLine("<title>Color Palette</title>");
            htmlContent.AppendLine("<style>");
            htmlContent.AppendLine("body { font-family: Arial, sans-serif; }");
            htmlContent.AppendLine("table { width: 100%; border-collapse: collapse; }");
            htmlContent.AppendLine("td { padding: 20px; text-align: center; color: white; text-shadow: 1px 1px 2px black; }");
            htmlContent.AppendLine("</style>");
            htmlContent.AppendLine("</head>");
            htmlContent.AppendLine("<body>");
            htmlContent.AppendLine("<h1>Color Palette</h1>");
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

        // Экспорт в ACO (Photoshop)
        public byte[] ExportToAco(string[] colors)
        {
            using (var memoryStream = new MemoryStream())
            using (var writer = new BinaryWriter(memoryStream))
            {
                // Заголовок (версия и количество цветов)
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

        // Вспомогательный метод для преобразования HEX в RGB
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
 
