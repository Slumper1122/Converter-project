namespace Converter.Logging;

public sealed class ConsoleStepLogger : IStepLogger
{
    public void Info(string step, string message) =>
        Console.WriteLine($"[{step}] {message}");

    public void Error(string step, string message) =>
        Console.Error.WriteLine($"[{step}] ERROR: {message}");
}
