namespace Converter.Logging;

public interface IStepLogger
{
    void Info(string step, string message);

    void Error(string step, string message);
}
