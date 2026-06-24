# CSV → TXT Konverter

Egy könnyűsúlyú, parancssoros (CLI) C# alkalmazás, amely egy maximum 1 MB nagyságú `.csv` fájlt tabulátorral elválasztott `.txt` fájllá konvertál. A projekt szigorúan külső függőségek (3rd party NuGet csomagok és .dll-ek) nélkül készült.

## Követelmények

- [.NET 8.0 SDK (LTS)](https://dotnet.microsoft.com/download/dotnet/8.0) 

## Build (Telepítés)

A projekt úgy van konfigurálva, hogy a `publish` parancs hatására automatikusan egyetlen `.exe` állományt hozzon létre (Single-file deployment), amihez csak a gépen lévő .NET 8 környezet szükséges.

```bash
dotnet publish Converter/Converter.csproj -c Release
```

A kimeneti futtatható fájl itt jön létre:
Converter/bin/Release/net8.0/publish/Converter.exe

Használat
A program terminálban használható, és pontosan 1 argumentumot vár: a bemeneti CSV fájl elérési útját. A CSV fájl mérete nem haladhatja meg az 1 MB-ot.

```bash
Converter.exe adatok.csv
```

Kimenet: A program ugyanabban a mappában (az .exe mellett) létrehozza az adatok.txt fájlt, és felülírja azt, ha már létezik. A mezők tabulátorral (\t) lesznek elválasztva.

Példa:

adatok.csv (bemenet)

```

Nev,Kor,Varos
Anna,25,Budapest
Bela,30,Debrecen
```

adatok.txt (kimenet)

```

Nev	Kor	Varos
Anna	25	Budapest
Bela	30	Debrecen
```

Tesztek
A projekt nem használ külső keretrendszert (pl. xUnit), hanem egy saját, pofonegyszerű és gyors TestRunner implementációval rendelkezik, amely az alábbi parnccsal futtatható:

```bash
dotnet run --project Converter.Tests/Converter.Tests.csproj -c Release
```

Tartalmazott tesztesetek (5 db):
Teszt NévMit ellenőrizConvert_ValidCsv_CreatesTabSeparatedTxtFileNextToExe	Normál CSV sikeres konvertálása TXT-be
Convert_CsvWithQuotedFields_PreservesCommasInsideQuotes		Idézőjelek közé zárt, vesszőt tartalmazó mezők (RFC 4180)
Convert_MissingFile_ThrowsFileNotFoundException		Nem létező fájl esetén a megfelelő kivétel dobása
Convert_FileTooLarge_ThrowsInvalidOperationException 	Az 1 MB-os fájlméret-limit biztonsági ellenőrzése
Convert_ExistingTxtFile_IsOverwritten	 Meglévő célfájl helyes felülírása

Projekt struktúra

Converter/
├── Converter/              # A fő CLI alkalmazás és az üzleti logika
│   ├── Program.cs
│   ├── CsvToTxtConverter.cs
│   └── Converter.csproj    # Single-file publish beállításokkal
├── Converter.Tests/        # Saját implementációjú tesztkörnyezet
│   ├── Program.cs          # TestRunner és Assert logikák
│   └── Converter.Tests.csproj
├── minta.csv               # Példafájl a teszteléshez
├── Converter.slnx          # Visual Studio 2022 / Rider solution fájl
└── README.md

Gyors indítás (Klónozás után)
Ha frissen töltöd le a repót, az alábbi parancsokkal tudod azonnal tesztelni és lefordítani:
```bash
git clone [https://github.com/Slumper1122/Converter-project.git](https://github.com/Slumper1122/Converter-project.git)
cd Converter-project
dotnet run --project Converter.Tests/Converter.Tests.csproj -c Release
dotnet publish Converter/Converter.csproj -c Release
Converter/bin/Release/net8.0/publish/Converter.exe minta.csv
```
