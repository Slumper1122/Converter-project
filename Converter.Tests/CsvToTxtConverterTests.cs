using Converter;

namespace Converter.Tests;

public class CsvToTxtConverterTests
{
    [Fact]
    public void Convert_ValidCsv_CreatesTabSeparatedTxtFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var csvPath = Path.Combine(tempDir, "adatok.csv");
            File.WriteAllLines(csvPath, ["Nev,Kor,Varos", "Anna,25,Budapest", "Bela,30,Debrecen"]);

            var outputPath = CsvToTxtConverter.Convert(csvPath);

            Assert.Equal(Path.Combine(tempDir, "adatok.txt"), outputPath);
            Assert.True(File.Exists(outputPath));

            var lines = File.ReadAllLines(outputPath);
            Assert.Equal(3, lines.Length);
            Assert.Equal("Nev\tKor\tVaros", lines[0]);
            Assert.Equal("Anna\t25\tBudapest", lines[1]);
            Assert.Equal("Bela\t30\tDebrecen", lines[2]);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Convert_CsvWithQuotedFields_PreservesCommasInsideQuotes()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var csvPath = Path.Combine(tempDir, "idezet.csv");
            File.WriteAllText(csvPath, "Cim,Leiras" + Environment.NewLine + "\"Hello, world\",\"Egy \"\"szep\"\" nap\"");

            var outputPath = CsvToTxtConverter.Convert(csvPath);
            var lines = File.ReadAllLines(outputPath);

            Assert.Equal("Cim\tLeiras", lines[0]);
            Assert.Equal("Hello, world\tEgy \"szep\" nap", lines[1]);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Convert_MissingFile_ThrowsFileNotFoundException()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".csv");

        var exception = Assert.Throws<FileNotFoundException>(() => CsvToTxtConverter.Convert(missingPath));

        Assert.Contains(missingPath, exception.Message);
    }
}
