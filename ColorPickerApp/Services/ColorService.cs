using System;
using System.Collections.Generic;
using System.Drawing; // Для System.Drawing.Color
using System.Globalization; // Для NumberStyles.HexNumber
using System.Linq; // Для .Select, .ToList, .Distinct

namespace ColorPickerApp.Services
{
    public struct HslColor
    {
        public double H { get; set; } // Оттенок (Hue) 0-360
        public double S { get; set; } // Насыщенность (Saturation) 0-1
        public double L { get; set; } // Светлота (Lightness) 0-1
        public double A { get; set; } // Альфа (прозрачность) 0-1
    }

    public static class ColorService // Ваш класс остается статическим
    {
        // --- Конвертеры Цветов (из PaletteGeneratorService) ---

        public static Color HexToRgb(string hexColor)
        {
            hexColor = hexColor.TrimStart('#');
            if (hexColor.Length == 8) // с альфа-каналом
            {
                return Color.FromArgb(
                    int.Parse(hexColor.Substring(6, 2), NumberStyles.HexNumber), // Alpha
                    int.Parse(hexColor.Substring(0, 2), NumberStyles.HexNumber), // R
                    int.Parse(hexColor.Substring(2, 2), NumberStyles.HexNumber), // G
                    int.Parse(hexColor.Substring(4, 2), NumberStyles.HexNumber)  // B
                );
            }
            if (hexColor.Length == 6) // без альфа
            {
                return Color.FromArgb(
                    255, // Alpha (полностью непрозрачный)
                    int.Parse(hexColor.Substring(0, 2), NumberStyles.HexNumber),
                    int.Parse(hexColor.Substring(2, 2), NumberStyles.HexNumber),
                    int.Parse(hexColor.Substring(4, 2), NumberStyles.HexNumber)
                );
            }
            if (hexColor.Length == 3) // короткий формат #RGB
            {
                return Color.FromArgb(
                    255,
                    int.Parse(hexColor[0].ToString() + hexColor[0].ToString(), NumberStyles.HexNumber),
                    int.Parse(hexColor[1].ToString() + hexColor[1].ToString(), NumberStyles.HexNumber),
                    int.Parse(hexColor[2].ToString() + hexColor[2].ToString(), NumberStyles.HexNumber)
                );
            }

            throw new ArgumentException("Неверный формат HEX строки.", nameof(hexColor));
        }

        public static string RgbToHex(Color color, bool includeAlpha = false)
        {
            if (includeAlpha && color.A < 255) // Включаем альфу только если она не полная
            {
                return $"#{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}";
            }
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        public static HslColor RgbToHsl(Color rgb)
        {
            double r = rgb.R / 255.0;
            double g = rgb.G / 255.0;
            double b = rgb.B / 255.0;
            double a = rgb.A / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            double h = 0, s = 0, l = (max + min) / 2.0;

            if (delta > 0.00001) // Используем небольшое значение для сравнения double
            {
                s = (l < 0.5) ? (delta / (max + min)) : (delta / (2.0 - max - min));

                if (Math.Abs(r - max) < 0.00001) h = (g - b) / delta + (g < b ? 6 : 0); // Добавлено + (g < b ? 6 : 0) для правильного круга
                else if (Math.Abs(g - max) < 0.00001) h = 2.0 + (b - r) / delta;
                else h = 4.0 + (r - g) / delta;

                h *= 60;
                // if (h < 0) h += 360; // Уже учтено в (g < b ? 6 : 0)
            }
            else // Если delta == 0, то это серый цвет
            {
                h = 0; // Оттенок не имеет значения для серого
                s = 0; // Насыщенность 0
            }
            return new HslColor { H = h, S = s, L = l, A = a };
        }



        public static Color HslToRgb(HslColor hsl)
        {
            double r, g, b;
            double h = hsl.H;
            double s = hsl.S;
            double l = hsl.L;

            if (Math.Abs(s) < 0.00001) // Если насыщенность почти 0
            {
                r = g = b = l; // ахроматический цвет
            }
            else
            {
                Func<double, double, double, double> HueToComponent = (p, q, t) =>
                {
                    if (t < 0) t += 1;
                    if (t > 1) t -= 1;
                    if (t < 1.0 / 6) return p + (q - p) * 6 * t;
                    if (t < 1.0 / 2) return q;
                    if (t < 2.0 / 3) return p + (q - p) * (2.0 / 3 - t) * 6;
                    return p;
                };

                double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
                double p = 2 * l - q;
                double hk = h / 360.0; // Нормализованный оттенок (0-1)

                r = HueToComponent(p, q, hk + 1.0 / 3);
                g = HueToComponent(p, q, hk);
                b = HueToComponent(p, q, hk - 1.0 / 3);
            }

            return Color.FromArgb(
                (int)Math.Round(hsl.A * 255), // Учитываем альфу
                (int)Math.Clamp(Math.Round(r * 255), 0, 255), // Clamp для предотвращения выхода за пределы
                (int)Math.Clamp(Math.Round(g * 255), 0, 255),
                (int)Math.Clamp(Math.Round(b * 255), 0, 255)
            );
        }

        public static List<string> GenerateSimplePalette(string baseHexColor)
        {
            var palette = new List<string>();
            if (string.IsNullOrEmpty(baseHexColor)) return palette; // Возвращаем пустую палитру, если нет базового цвета

            // Убедимся, что HEX всегда с # для внутренних операций
            string internalBaseHex = baseHexColor.StartsWith("#") ? baseHexColor : "#" + baseHexColor;
            palette.Add(internalBaseHex); // Добавляем исходный цвет

            try
            {
                // Аналогичные цвета (как в вашем старом AdjustHue)
                palette.Add(AdjustHue(internalBaseHex, 30));
                palette.Add(AdjustHue(internalBaseHex, -30));

                // Оттенки основного цвета (как в вашем старом AdjustBrightness)
                palette.Add(AdjustBrightness(internalBaseHex, 20)); // Уменьшил процент для более тонких оттенков
                palette.Add(AdjustBrightness(internalBaseHex, -20));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при генерации простой палитры для {internalBaseHex}: {ex.Message}");
                // В случае ошибки, возвращаем только базовый цвет, который уже добавлен
            }
            return palette.Distinct().ToList(); // Убираем дубликаты
        }

        // Основной метод для генерации всех типов палитр
        public static Dictionary<string, List<string>> GenerateAllHarmonies(string baseHexColor)
        {
            var allPalettes = new Dictionary<string, List<string>>();
            Color baseRgb;
            HslColor baseHsl;

            string internalBaseHex = baseHexColor.StartsWith("#") ? baseHexColor : "#" + baseHexColor;

            try
            {
                baseRgb = HexToRgb(internalBaseHex);
                baseHsl = RgbToHsl(baseRgb); // Используем новый RgbToHsl
                baseHsl.A = 1.0; // Убедимся, что альфа = 1 для генерации непрозрачных палитр
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Ошибка парсинга HEX в GenerateAllHarmonies: {ex.Message} для цвета {internalBaseHex}");
                allPalettes["error"] = new List<string> { "Неверный базовый HEX цвет: " + internalBaseHex };
                return allPalettes;
            }

            // Базовая палитра (сам цвет)
            allPalettes["base"] = new List<string> { internalBaseHex };

            // Монохромная палитра
            var monochromatic = new List<string> { internalBaseHex };
            for (int i = 1; i <= 2; i++)
            {
                HslColor lighter = baseHsl; lighter.L = Math.Clamp(baseHsl.L + i * 0.15, 0.0, 1.0);
                monochromatic.Add(RgbToHex(HslToRgb(lighter)));
                HslColor darker = baseHsl; darker.L = Math.Clamp(baseHsl.L - i * 0.15, 0.0, 1.0);
                monochromatic.Insert(0, RgbToHex(HslToRgb(darker)));
            }
            allPalettes["monochromatic"] = monochromatic.Distinct().ToList();

            // Аналогичная палитра
            var analogous = new List<string> { internalBaseHex };
            HslColor analogous1 = baseHsl; analogous1.H = (baseHsl.H - 30 + 360) % 360;
            HslColor analogous2 = baseHsl; analogous2.H = (baseHsl.H + 30 + 360) % 360;
            analogous.Insert(0, RgbToHex(HslToRgb(analogous1)));
            analogous.Add(RgbToHex(HslToRgb(analogous2)));
            allPalettes["analogous"] = analogous.Distinct().ToList();

            // Комплементарная палитра
            HslColor complementaryHsl = baseHsl; complementaryHsl.H = (baseHsl.H + 180 + 360) % 360;
            allPalettes["complementary"] = new List<string> { internalBaseHex, RgbToHex(HslToRgb(complementaryHsl)) }.Distinct().ToList();

            // Расщепленно-комплементарная
            var splitComplementary = new List<string> { internalBaseHex };
            HslColor split1 = baseHsl; split1.H = (baseHsl.H + 180 - 30 + 360) % 360; // 150 градусов от комплементарного
            HslColor split2 = baseHsl; split2.H = (baseHsl.H + 180 + 30 + 360) % 360;

            splitComplementary.Add(RgbToHex(HslToRgb(split1)));
            splitComplementary.Add(RgbToHex(HslToRgb(split2)));
            allPalettes["splitComplementary"] = splitComplementary.Distinct().ToList();

            // Триадная палитра
            var triadic = new List<string> { internalBaseHex };
            HslColor triadic1 = baseHsl; triadic1.H = (baseHsl.H + 120 + 360) % 360;
            HslColor triadic2 = baseHsl; triadic2.H = (baseHsl.H + 240 + 360) % 360;
            triadic.Add(RgbToHex(HslToRgb(triadic1)));
            triadic.Add(RgbToHex(HslToRgb(triadic2)));
            allPalettes["triadic"] = triadic.Distinct().ToList();

            // Тетрадная (прямоугольная/квадратная) палитра
            var tetradic = new List<string> { internalBaseHex };
            HslColor tetradicSqr1 = baseHsl; tetradicSqr1.H = (baseHsl.H + 90 + 360) % 360;
            HslColor tetradicSqr2 = baseHsl; tetradicSqr2.H = (baseHsl.H + 180 + 360) % 360;
            HslColor tetradicSqr3 = baseHsl; tetradicSqr3.H = (baseHsl.H + 270 + 360) % 360;
            tetradic.Add(RgbToHex(HslToRgb(tetradicSqr1)));
            tetradic.Add(RgbToHex(HslToRgb(tetradicSqr2)));
            tetradic.Add(RgbToHex(HslToRgb(tetradicSqr3)));
            allPalettes["tetradic (square)"] = tetradic.Distinct().ToList();

            // Генерация цветов текста (черный/белый для контраста)
            var textColors = new Dictionary<string, string>();
            foreach (var paletteKvp in allPalettes.ToList()) // ToList(), чтобы можно было изменять allPalettes, если нужно
            {
                foreach (var colorHex in paletteKvp.Value)
                {
                    if (!textColors.ContainsKey(colorHex))
                    {
                        try
                        {
                            Color bgColor = HexToRgb(colorHex);
                            double luminance = (0.299 * bgColor.R + 0.587 * bgColor.G + 0.114 * bgColor.B) / 255.0;
                            textColors[colorHex] = luminance > 0.5 ? "#000000" : "#FFFFFF";
                        }
                        catch (ArgumentException ex)
                        {
                            Console.WriteLine($"Ошибка парсинга HEX для текстового цвета: {ex.Message} для {colorHex}");
                            textColors[colorHex] = "#000000"; // По умолчанию черный
                        }
                    }
                }
            }

            allPalettes["textContrast"] = textColors.Select(kvp => $"{kvp.Key}:{kvp.Value}").ToList();


            return allPalettes;
        }



        public static string AdjustBrightness(string hex, int percent)
        {
            string internalHex = hex.StartsWith("#") ? hex : "#" + hex;
            Color rgbColor;
            try
            {
                rgbColor = HexToRgb(internalHex);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"AdjustBrightness: Неверный HEX {internalHex} - {ex.Message}");
                return internalHex; // Возвращаем исходный цвет в случае ошибки
            }

            HslColor hsl = RgbToHsl(rgbColor);
            hsl.A = 1.0;

            // Изме
            double factor = percent / 100.0;
            hsl.L = Math.Clamp(hsl.L + factor, 0.0, 1.0);

            return RgbToHex(HslToRgb(hsl));
        }

        public static string AdjustHue(string hex, int degrees)
        {
            string internalHex = hex.StartsWith("#") ? hex : "#" + hex;
            Color rgbColor;
            try
            {
                rgbColor = HexToRgb(internalHex);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"AdjustHue: Неверный HEX {internalHex} - {ex.Message}");
                return internalHex; // Возвращаем исходный цвет в случае ошибки
            }

            HslColor hsl = RgbToHsl(rgbColor); // Используем новый RgbToHsl
            hsl.A = 1.0;

            hsl.H = (hsl.H + degrees + 360) % 360; // Сдвигаем оттенок

            return RgbToHex(HslToRgb(hsl));
        }
    }
}

