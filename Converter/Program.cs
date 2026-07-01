using Converter.Logging;
using Converter.Services;

if (args.Length != 2)
{
    Console.Error.WriteLine("[VALIDATE] ERROR: Expected exactly 2 arguments.");
    Console.Error.WriteLine("Usage: Converter <input-file-path> <target-format>");
    Console.Error.WriteLine("Target format: csv | txt | json");
    Console.Error.WriteLine("Example: Converter data.csv json");
    return 1;
}

var logger = new ConsoleStepLogger();
var converter = new FileConverterService(logger);

try
{
    var outputPath = converter.Convert(args[0], args[1]);
    logger.Info("RESULT", $"Output file: {outputPath}");
    return 0;
}
catch (Exception ex)
{
    logger.Error("FAILED", ex.Message);
    return 1;
}
