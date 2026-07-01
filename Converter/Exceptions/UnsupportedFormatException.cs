namespace Converter.Exceptions;

public class UnsupportedFormatException : Exception
{
    public UnsupportedFormatException(string message) : base(message)
    {
    }

    public UnsupportedFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
