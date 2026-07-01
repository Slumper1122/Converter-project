using Converter.Exceptions;
using Converter.Models;
using Converter.Parsing;

namespace Converter.Tests;

public class FormatParserTests
{
    [Theory]
    [InlineData("data.csv", FileFormat.Csv)]
    [InlineData("data.TXT", FileFormat.Txt)]
    [InlineData("data.json", FileFormat.Json)]
    public void ParseFromExtension_ReturnsExpectedFormat(string path, FileFormat expected)
    {
        Assert.Equal(expected, FormatParser.ParseFromExtension(path));
    }

    [Fact]
    public void ParseFromExtension_UnknownExtension_Throws()
    {
        Assert.Throws<UnsupportedFormatException>(() => FormatParser.ParseFromExtension("file.xml"));
    }

    [Theory]
    [InlineData("csv", FileFormat.Csv)]
    [InlineData("TXT", FileFormat.Txt)]
    [InlineData("json", FileFormat.Json)]
    public void ParseFromName_ReturnsExpectedFormat(string name, FileFormat expected)
    {
        Assert.Equal(expected, FormatParser.ParseFromName(name));
    }

    [Fact]
    public void ParseFromName_EmptyName_Throws()
    {
        Assert.Throws<UnsupportedFormatException>(() => FormatParser.ParseFromName(" "));
    }

    [Theory]
    [InlineData(FileFormat.Csv, ".csv")]
    [InlineData(FileFormat.Txt, ".txt")]
    [InlineData(FileFormat.Json, ".json")]
    public void ToExtension_ReturnsExpectedValue(FileFormat format, string expected)
    {
        Assert.Equal(expected, FormatParser.ToExtension(format));
    }
}

public class CsvLineParserTests
{
    [Fact]
    public void Parse_EmptyLine_ReturnsEmptyArray()
    {
        Assert.Empty(CsvLineParser.Parse(string.Empty));
    }

    [Fact]
    public void Format_FieldWithComma_IsQuoted()
    {
        Assert.Equal("\"a,b\"", CsvLineParser.Format(["a,b"]));
    }
}
