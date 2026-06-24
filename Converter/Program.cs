using Converter;

if (args.Length != 1)
{
    Console.Error.WriteLine("Használat: Converter <csv-fájl-útvonal>");
    Console.Error.WriteLine("Példa: Converter adatok.csv");
    return 1;
}

try
{
    var outputPath = CsvToTxtConverter.Convert(args[0]);
    Console.WriteLine($"Sikeres konverzió: {outputPath}");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Hiba: {ex.Message}");
    return 1;
}
