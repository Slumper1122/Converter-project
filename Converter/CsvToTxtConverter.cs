namespace Converter;

public static class CsvToTxtConverter
{
    public static string Convert(string csvFilePath)
    {
        if (string.IsNullOrWhiteSpace(csvFilePath))
        {
            throw new ArgumentException("A CSV fájl elérési útja nem lehet üres.", nameof(csvFilePath));
        }

        if (!File.Exists(csvFilePath))
        {
            throw new FileNotFoundException($"A megadott CSV fájl nem található: {csvFilePath}", csvFilePath);
        }

        var outputPath = Path.ChangeExtension(csvFilePath, ".txt");
        var lines = File.ReadAllLines(csvFilePath);
        var outputLines = lines.Select(ParseCsvLine).Select(fields => string.Join('\t', fields));

        File.WriteAllLines(outputPath, outputLines);
        return outputPath;
    }

    internal static string[] ParseCsvLine(string line)
    {
        if (string.IsNullOrEmpty(line))
        {
            return [];
        }

        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (c == ',' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        fields.Add(current.ToString());
        return fields.ToArray();
    }
}
