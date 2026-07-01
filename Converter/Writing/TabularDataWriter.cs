using Converter.Exceptions;
using Converter.Models;
using Converter.Parsing;
using Newtonsoft.Json;

namespace Converter.Writing;

public static class TabularDataWriter
{
    public static string Write(TabularData data, FileFormat format)
    {
        return format switch
        {
            FileFormat.Csv => WriteDelimited(data, ','),
            FileFormat.Txt => WriteDelimited(data, '\t'),
            FileFormat.Json => WriteJson(data),
            _ => throw new UnsupportedFormatException($"Cannot write format '{format}'.")
        };
    }

    private static string WriteDelimited(TabularData data, char delimiter)
    {
        var lines = new List<string>();
        lines.Add(delimiter == ','
            ? CsvLineParser.Format(data.Headers)
            : string.Join(delimiter, data.Headers));

        foreach (var row in data.Rows)
        {
            if (row.Count != data.Headers.Count)
            {
                throw new UnsupportedFormatException("Row column count does not match header count.");
            }

            lines.Add(delimiter == ','
                ? CsvLineParser.Format(row)
                : string.Join(delimiter, row));
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string WriteJson(TabularData data)
    {
        var records = data.Rows
            .Select(row => data.Headers
                .Select((header, index) => new KeyValuePair<string, string>(header, row[index]))
                .ToDictionary(pair => pair.Key, pair => pair.Value))
            .ToList();

        return JsonConvert.SerializeObject(records, Formatting.Indented);
    }
}
