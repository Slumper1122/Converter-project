using Converter.Exceptions;
using Converter.Models;

namespace Converter.Parsing;

public static class FormatParser
{
    private static readonly Dictionary<string, FileFormat> ExtensionMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".csv"] = FileFormat.Csv,
            [".txt"] = FileFormat.Txt,
            [".json"] = FileFormat.Json
        };

    public static FileFormat ParseFromExtension(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        if (string.IsNullOrWhiteSpace(extension) || !ExtensionMap.TryGetValue(extension, out var format))
        {
            throw new UnsupportedFormatException(
                $"Unsupported source format '{extension}'. Supported extensions: .csv, .txt, .json.");
        }

        return format;
    }

    public static FileFormat ParseFromName(string formatName)
    {
        if (string.IsNullOrWhiteSpace(formatName))
        {
            throw new UnsupportedFormatException("Target format cannot be empty.");
        }

        return formatName.Trim().ToLowerInvariant() switch
        {
            "csv" => FileFormat.Csv,
            "txt" => FileFormat.Txt,
            "json" => FileFormat.Json,
            _ => throw new UnsupportedFormatException(
                $"Unsupported target format '{formatName}'. Supported values: csv, txt, json.")
        };
    }

    public static string ToExtension(FileFormat format) =>
        format switch
        {
            FileFormat.Csv => ".csv",
            FileFormat.Txt => ".txt",
            FileFormat.Json => ".json",
            _ => throw new UnsupportedFormatException($"Unknown format '{format}'.")
        };
}
