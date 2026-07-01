namespace Converter.Models;

public sealed class TabularData
{
    public TabularData(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        Headers = headers;
        Rows = rows;
    }

    public IReadOnlyList<string> Headers { get; }

    public IReadOnlyList<IReadOnlyList<string>> Rows { get; }
}
