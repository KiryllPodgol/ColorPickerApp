using System.Text;
using System.Text.RegularExpressions;

public class ImportService
{
    public List<string> Import(Stream stream, string extension)
    {
        extension = extension.ToLowerInvariant();

        return extension switch
        {
            ".txt" => ImportFromTxt(stream),
            ".html" or ".htm" => ImportFromHtml(stream),
            ".css" => ImportFromCss(stream),
            ".aco" => ImportFromAco(stream),
            ".gpl" => ImportFromGimp(stream),
            _ => throw new NotSupportedException($"Формат {extension} не поддерживается")
        };
    }

    public List<string> ImportFromTxt(Stream stream)
    {
        var colors = new List<string>();
        using var reader = new StreamReader(stream);
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine()?.Trim();
            if (!string.IsNullOrWhiteSpace(line))
                colors.Add(line);
        }
        return colors;
    }

    public List<string> ImportFromHtml(Stream stream)
    {
        using var reader = new StreamReader(stream);
        string content = reader.ReadToEnd();
        return ExtractColorsFromText(content);
    }

    public List<string> ImportFromCss(Stream stream)
    {
        using var reader = new StreamReader(stream);
        string content = reader.ReadToEnd();
        return ExtractColorsFromText(content);
    }

    private List<string> ExtractColorsFromText(string content)
    {
        var colors = new List<string>();

        // HEX: #RGB, #RRGGBB, #ARGB, #AARRGGBB
        var hexMatches = Regex.Matches(content, @"#([A-Fa-f0-9]{3,8})\b");
        foreach (Match match in hexMatches)
        {
            colors.Add(match.Value.ToUpperInvariant());
        }

        // rgb() / rgba()
        var rgbMatches = Regex.Matches(content, @"rgba?\(\s*(\d{1,3})\s*,\s*(\d{1,3})\s*,\s*(\d{1,3})");
        foreach (Match match in rgbMatches)
        {
            int r = int.Parse(match.Groups[1].Value);
            int g = int.Parse(match.Groups[2].Value);
            int b = int.Parse(match.Groups[3].Value);

            colors.Add($"#{r:X2}{g:X2}{b:X2}");
        }

        return colors.Distinct().ToList();
    }

    public List<string> ImportFromGimp(Stream stream)
    {
        var colors = new List<string>();

        using var reader = new StreamReader(stream);
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 3 &&
                int.TryParse(parts[0], out int r) &&
                int.TryParse(parts[1], out int g) &&
                int.TryParse(parts[2], out int b))
            {
                r = Math.Clamp(r, 0, 255);
                g = Math.Clamp(g, 0, 255);
                b = Math.Clamp(b, 0, 255);

                colors.Add($"#{r:X2}{g:X2}{b:X2}");
            }
        }

        return colors.Distinct().ToList();
    }

    public List<string> ImportFromAco(Stream stream)
    {
        var colors = new List<string>();
        using var reader = new BinaryReader(stream);

        // Skip header
        int version = reader.ReadInt16();
        int count = reader.ReadInt16();

        for (int i = 0; i < count; i++)
        {
            int colorSpace = reader.ReadInt16(); // 0 = RGB
            int r = reader.ReadInt16() / 256;
            int g = reader.ReadInt16() / 256;
            int b = reader.ReadInt16() / 256;
            reader.BaseStream.Seek(2, SeekOrigin.Current); // Skip padding

            if (colorSpace == 0)
            {
                r = Math.Clamp(r, 0, 255);
                g = Math.Clamp(g, 0, 255);
                b = Math.Clamp(b, 0, 255);

                colors.Add($"#{r:X2}{g:X2}{b:X2}");
            }
        }

        return colors.Distinct().ToList();
    }
}
