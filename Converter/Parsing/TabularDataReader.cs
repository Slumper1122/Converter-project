using Converter.Exceptions;
using Converter.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Converter.Parsing;

public static class TabularDataReader
{
    public static TabularData Read(string filePath, FileFormat format)
    {
        var lines = File.ReadAllLines(filePath);

        return format switch
        {
            FileFormat.Csv => ReadDelimited(lines, ',', "CSV"),
            FileFormat.Txt => ReadDelimited(lines, '\t', "TXT"),
            FileFormat.Json => ReadJson(File.ReadAllText(filePath)),
            _ => throw new UnsupportedFormatException($"Cannot read format '{format}'.")
        };
    }

    private static TabularData ReadDelimited(string[] lines, char delimiter, string label)
    {
        if (lines.Length == 0)
        {
            throw new UnsupportedFormatException($"{label} file is empty.");
        }

        var rows = lines.Select(line => SplitDelimitedLine(line, delimiter)).ToList();
        if (rows[0].Count == 0)
        {
            throw new UnsupportedFormatException($"{label} header row is invalid.");
        }

        var headers = rows[0];
        var dataRows = rows.Skip(1).ToList();
        return new TabularData(headers, dataRows);
    }

    private static IReadOnlyList<string> SplitDelimitedLine(string line, char delimiter)
    {
        if (delimiter == ',')
        {
            return CsvLineParser.Parse(line);
        }

        if (string.IsNullOrEmpty(line))
        {
            return [];
        }

        return line.Split('\t');
    }

    private static TabularData ReadJson(string content)
    {
        try
        {
            var token = JToken.Parse(content);
            if (token is not JArray array)
            {
                throw new UnsupportedFormatException("JSON root must be an array of objects.");
            }

            if (array.Count == 0)
            {
                return new TabularData([], []);
            }

            var headers = array
                .OfType<JObject>()
                .SelectMany(obj => obj.Properties())
                .Select(property => property.Name)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (headers.Count == 0)
            {
                throw new UnsupportedFormatException("JSON array must contain objects with properties.");
            }

            var rows = new List<IReadOnlyList<string>>();
            foreach (var item in array)
            {
                if (item is not JObject obj)
                {
                    throw new UnsupportedFormatException("Each JSON item must be an object.");
                }

                rows.Add(headers.Select(header => obj[header]?.ToString() ?? string.Empty).ToList());
            }

            return new TabularData(headers, rows);
        }
        catch (JsonException ex)
        {
            throw new UnsupportedFormatException("Invalid JSON content.", ex);
        }
    }
}
