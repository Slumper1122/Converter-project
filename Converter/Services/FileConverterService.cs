using Converter.Exceptions;
using Converter.Logging;
using Converter.Models;
using Converter.Parsing;
using Converter.Writing;

namespace Converter.Services;

public sealed class FileConverterService
{
    public const int MaxFileSizeBytes = 1_048_576;

    private readonly IStepLogger _logger;

    public FileConverterService(IStepLogger logger)
    {
        _logger = logger;
    }

    public string Convert(string inputFilePath, string targetFormatName, string? outputDirectory = null)
    {
        _logger.Info("VALIDATE", "Checking input arguments and file constraints.");

        if (string.IsNullOrWhiteSpace(inputFilePath))
        {
            throw new ArgumentException("Input file path cannot be empty.", nameof(inputFilePath));
        }

        if (!File.Exists(inputFilePath))
        {
            throw new FileNotFoundException($"Input file not found: {inputFilePath}", inputFilePath);
        }

        var fileInfo = new FileInfo(inputFilePath);
        if (fileInfo.Length > MaxFileSizeBytes)
        {
            throw new InvalidOperationException(
                $"Input file size ({fileInfo.Length} bytes) exceeds the 1 MB limit.");
        }

        _logger.Info("DETECT", "Detecting source format from file extension.");
        var sourceFormat = FormatParser.ParseFromExtension(inputFilePath);

        _logger.Info("RESOLVE", "Resolving target format.");
        var targetFormat = FormatParser.ParseFromName(targetFormatName);

        if (sourceFormat == targetFormat)
        {
            throw new UnsupportedFormatException(
                $"Source and target format are both '{targetFormat}'. Choose a different target format.");
        }

        _logger.Info("READ", $"Reading {sourceFormat} content from '{Path.GetFileName(inputFilePath)}'.");
        var tabularData = TabularDataReader.Read(inputFilePath, sourceFormat);

        _logger.Info("TRANSFORM", $"Converting {sourceFormat} -> {targetFormat}.");
        var outputContent = TabularDataWriter.Write(tabularData, targetFormat);

        outputDirectory ??= AppContext.BaseDirectory;
        var outputPath = Path.Combine(
            outputDirectory,
            Path.ChangeExtension(Path.GetFileName(inputFilePath), FormatParser.ToExtension(targetFormat)));

        _logger.Info("WRITE", $"Writing output to '{outputPath}'.");
        File.WriteAllText(outputPath, outputContent);

        _logger.Info("DONE", $"Conversion completed successfully ({sourceFormat} -> {targetFormat}).");
        return outputPath;
    }
}
