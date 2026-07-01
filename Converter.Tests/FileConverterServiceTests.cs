using Converter.Exceptions;
using Converter.Logging;
using Converter.Services;

namespace Converter.Tests;

public class FileConverterServiceTests
{
    private static FileConverterService CreateService() =>
        new(new NullStepLogger());

    [Fact]
    public void Convert_CsvToTxt_WritesTabSeparatedOutputNextToExe()
    {
        var tempDir = CreateTempDir();

        try
        {
            var csvPath = Path.Combine(tempDir, "data.csv");
            File.WriteAllLines(csvPath, ["Name,Age,City", "Anna,25,Budapest"]);

            var outputPath = CreateService().Convert(csvPath, "txt", tempDir);

            Assert.Equal(Path.Combine(tempDir, "data.txt"), outputPath);
            Assert.Equal("Name\tAge\tCity" + Environment.NewLine + "Anna\t25\tBudapest", File.ReadAllText(outputPath).TrimEnd());
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Convert_TxtToCsv_WritesCommaSeparatedOutput()
    {
        var tempDir = CreateTempDir();

        try
        {
            var txtPath = Path.Combine(tempDir, "data.txt");
            File.WriteAllText(txtPath, "Name\tAge" + Environment.NewLine + "Bob\t30");

            var outputPath = CreateService().Convert(txtPath, "csv", tempDir);
            var lines = File.ReadAllLines(outputPath);

            Assert.Equal("Name,Age", lines[0]);
            Assert.Equal("Bob,30", lines[1]);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Convert_CsvToJson_WritesJsonArray()
    {
        var tempDir = CreateTempDir();

        try
        {
            var csvPath = Path.Combine(tempDir, "data.csv");
            File.WriteAllText(csvPath, "Name,Age" + Environment.NewLine + "Anna,25");

            var outputPath = CreateService().Convert(csvPath, "json", tempDir);
            var content = File.ReadAllText(outputPath);

            Assert.Contains("\"Name\": \"Anna\"", content);
            Assert.Contains("\"Age\": \"25\"", content);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Convert_JsonToCsv_WritesCsvRows()
    {
        var tempDir = CreateTempDir();

        try
        {
            var jsonPath = Path.Combine(tempDir, "data.json");
            File.WriteAllText(jsonPath, "[{\"Name\":\"Anna\",\"Age\":\"25\"}]");

            var outputPath = CreateService().Convert(jsonPath, "csv", tempDir);
            var lines = File.ReadAllLines(outputPath);

            Assert.Equal("Name,Age", lines[0]);
            Assert.Equal("Anna,25", lines[1]);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Convert_JsonToTxt_WritesTabSeparatedRows()
    {
        var tempDir = CreateTempDir();

        try
        {
            var jsonPath = Path.Combine(tempDir, "data.json");
            File.WriteAllText(jsonPath, "[{\"Name\":\"Anna\",\"Age\":\"25\"}]");

            var outputPath = CreateService().Convert(jsonPath, "txt", tempDir);
            var lines = File.ReadAllLines(outputPath);

            Assert.Equal("Name\tAge", lines[0]);
            Assert.Equal("Anna\t25", lines[1]);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Convert_TxtToJson_WritesJsonArray()
    {
        var tempDir = CreateTempDir();

        try
        {
            var txtPath = Path.Combine(tempDir, "data.txt");
            File.WriteAllText(txtPath, "Name\tAge" + Environment.NewLine + "Anna\t25");

            var outputPath = CreateService().Convert(txtPath, "json", tempDir);

            Assert.Contains("\"Name\": \"Anna\"", File.ReadAllText(outputPath));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Convert_QuotedCsvFields_ArePreserved()
    {
        var tempDir = CreateTempDir();

        try
        {
            var csvPath = Path.Combine(tempDir, "quote.csv");
            File.WriteAllText(csvPath, "Title,Desc" + Environment.NewLine + "\"Hello, world\",\"A \"\"nice\"\" day\"");

            var outputPath = CreateService().Convert(csvPath, "txt", tempDir);
            var lines = File.ReadAllLines(outputPath);

            Assert.Equal("Hello, world\tA \"nice\" day", lines[1]);
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

        Assert.Throws<FileNotFoundException>(() => CreateService().Convert(missingPath, "txt", Path.GetTempPath()));
    }

    [Fact]
    public void Convert_FileTooLarge_ThrowsInvalidOperationException()
    {
        var tempDir = CreateTempDir();

        try
        {
            var csvPath = Path.Combine(tempDir, "large.csv");
            File.WriteAllBytes(csvPath, new byte[FileConverterService.MaxFileSizeBytes + 1]);

            var exception = Assert.Throws<InvalidOperationException>(() => CreateService().Convert(csvPath, "txt", tempDir));
            Assert.Contains("1 MB", exception.Message);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Convert_UnsupportedSourceExtension_ThrowsUnsupportedFormatException()
    {
        var tempDir = CreateTempDir();

        try
        {
            var xmlPath = Path.Combine(tempDir, "data.xml");
            File.WriteAllText(xmlPath, "<root/>");

            Assert.Throws<UnsupportedFormatException>(() => CreateService().Convert(xmlPath, "csv", tempDir));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Convert_UnsupportedTargetFormat_ThrowsUnsupportedFormatException()
    {
        var tempDir = CreateTempDir();

        try
        {
            var csvPath = Path.Combine(tempDir, "data.csv");
            File.WriteAllText(csvPath, "A,B\n1,2");

            Assert.Throws<UnsupportedFormatException>(() => CreateService().Convert(csvPath, "xml", tempDir));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Convert_SameSourceAndTarget_ThrowsUnsupportedFormatException()
    {
        var tempDir = CreateTempDir();

        try
        {
            var csvPath = Path.Combine(tempDir, "data.csv");
            File.WriteAllText(csvPath, "A,B\n1,2");

            Assert.Throws<UnsupportedFormatException>(() => CreateService().Convert(csvPath, "csv", tempDir));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Convert_InvalidJson_ThrowsUnsupportedFormatException()
    {
        var tempDir = CreateTempDir();

        try
        {
            var jsonPath = Path.Combine(tempDir, "bad.json");
            File.WriteAllText(jsonPath, "{not-json");

            Assert.Throws<UnsupportedFormatException>(() => CreateService().Convert(jsonPath, "csv", tempDir));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Convert_JsonRootNotArray_ThrowsUnsupportedFormatException()
    {
        var tempDir = CreateTempDir();

        try
        {
            var jsonPath = Path.Combine(tempDir, "object.json");
            File.WriteAllText(jsonPath, "{\"Name\":\"Anna\"}");

            Assert.Throws<UnsupportedFormatException>(() => CreateService().Convert(jsonPath, "csv", tempDir));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Convert_EmptyCsv_ThrowsUnsupportedFormatException()
    {
        var tempDir = CreateTempDir();

        try
        {
            var csvPath = Path.Combine(tempDir, "empty.csv");
            File.WriteAllText(csvPath, string.Empty);

            Assert.Throws<UnsupportedFormatException>(() => CreateService().Convert(csvPath, "txt", tempDir));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Convert_OverwritesExistingOutputFile()
    {
        var tempDir = CreateTempDir();

        try
        {
            var csvPath = Path.Combine(tempDir, "data.csv");
            var txtPath = Path.Combine(tempDir, "data.txt");
            File.WriteAllText(csvPath, "A,B\n1,2");
            File.WriteAllText(txtPath, "old content");

            CreateService().Convert(csvPath, "txt", tempDir);

            Assert.Equal("A\tB" + Environment.NewLine + "1\t2", File.ReadAllText(txtPath).TrimEnd());
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static string CreateTempDir()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }

    private sealed class NullStepLogger : IStepLogger
    {
        public void Info(string step, string message)
        {
        }

        public void Error(string step, string message)
        {
        }
    }
}
