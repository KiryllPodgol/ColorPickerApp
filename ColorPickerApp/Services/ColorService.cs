using System;
using System.Collections.Generic;

namespace ColorPickerApp.Services
{
    public static class ColorService
    {
        public static List<string> GeneratePalette(string baseColor)
        {
            var palette = new List<string> { baseColor };

            // Добавляем аналогичные цвета
            palette.Add(AdjustHue(baseColor, 30));
            palette.Add(AdjustHue(baseColor, -30));

            // Добавляем оттенки основного цвета
            palette.Add(AdjustBrightness(baseColor, 30));
            palette.Add(AdjustBrightness(baseColor, -30));

            return palette;
        }

        // Изменение яркости цвета
        private static string AdjustBrightness(string hex, int percent)
        {
            if (!hex.StartsWith("#")) hex = "#" + hex;

            int r = Convert.ToInt32(hex.Substring(1, 2), 16);
            int g = Convert.ToInt32(hex.Substring(3, 2), 16);
            int b = Convert.ToInt32(hex.Substring(5, 2), 16);

            if (percent < 0)
            {
                // Затемнение
                r = r * (100 + percent) / 100;
                g = g * (100 + percent) / 100;
                b = b * (100 + percent) / 100;
            }
            else
            {
                // Осветление
                r = r + ((255 - r) * percent / 100);
                g = g + ((255 - g) * percent / 100);
                b = b + ((255 - b) * percent / 100);
            }

            r = Math.Min(255, Math.Max(0, r));
            g = Math.Min(255, Math.Max(0, g));
            b = Math.Min(255, Math.Max(0, b));

            return $"#{r:X2}{g:X2}{b:X2}";
        }

        // Изменение оттенка цвета
        private static string AdjustHue(string hex, int degree)
        {
            if (!hex.StartsWith("#")) hex = "#" + hex;

            int r = Convert.ToInt32(hex.Substring(1, 2), 16);
            int g = Convert.ToInt32(hex.Substring(3, 2), 16);
            int b = Convert.ToInt32(hex.Substring(5, 2), 16);

            // Преобразуем RGB в HSL
            var (h, s, l) = RgbToHsl(r, g, b);

            // Изменяем оттенок
            h = (h + degree) % 360;
            if (h < 0) h += 360;

            // Преобразуем обратно в RGB
            (r, g, b) = HslToRgb(h, s, l);

            return $"#{r:X2}{g:X2}{b:X2}";
        }

        // Преобразование RGB в HSL
        private static (double h, double s, double l) RgbToHsl(int r, int g, int b)
        {
            double rf = r / 255.0;
            double gf = g / 255.0;
            double bf = b / 255.0;

            double max = Math.Max(rf, Math.Max(gf, bf));
            double min = Math.Min(rf, Math.Min(gf, bf));
            double delta = max - min;

            double h = 0;
            double s = 0;
            double l = (max + min) / 2.0;

            if (delta != 0)
            {
                s = l < 0.5 ? delta / (max + min) : delta / (2.0 - max - min);

                if (rf == max)
                    h = (gf - bf) / delta + (gf < bf ? 6 : 0);
                else if (gf == max)
                    h = (bf - rf) / delta + 2;
                else
                    h = (rf - gf) / delta + 4;

                h *= 60;
            }

            return (h, s, l);
        }

        // Преобразование HSL в RGB
        private static (int r, int g, int b) HslToRgb(double h, double s, double l)
        {
            double r, g, b;

            if (s == 0)
            {
                r = g = b = l;
            }
            else
            {
                double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
                double p = 2 * l - q;
                r = HueToRgb(p, q, h / 360 + 1.0 / 3);
                g = HueToRgb(p, q, h / 360);
                b = HueToRgb(p, q, h / 360 - 1.0 / 3);
            }

            return (
                r: Convert.ToInt32(Math.Round(r * 255)),
                g: Convert.ToInt32(Math.Round(g * 255)),
                b: Convert.ToInt32(Math.Round(b * 255))
            );
        }

        private static double HueToRgb(double p, double q, double t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1.0 / 6) return p + (q - p) * 6 * t;
            if (t < 1.0 / 2) return q;
            if (t < 2.0 / 3) return p + (q - p) * (2.0 / 3 - t) * 6;
            return p;
        }
    }
}