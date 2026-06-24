# CSV → TXT Konverter

Egyszerű parancssori C# program, amely egy CSV fájlt tabulátorral elválasztott TXT fájllá alakít.

## Követelmények

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) vagy újabb

## Build

```bash
dotnet build Converter.slnx
```

Release build (önálló futtatható fájl):

```bash
dotnet publish Converter/Converter.csproj -c Release -r win-x64 --self-contained false
```

A kimeneti exe:

```
Converter/bin/Release/net9.0/Converter.exe
```

Önálló (self-contained) exe, ha nincs telepítve .NET a gépen:

```bash
dotnet publish Converter/Converter.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

```
Converter/bin/Release/net9.0/win-x64/publish/Converter.exe
```

## Használat

A program **pontosan 1 argumentumot** vár: a bemeneti CSV fájl elérési útját.

```bash
Converter.exe adatok.csv
```

Kimenet: ugyanabban a mappában létrejön az `adatok.txt` fájl. A CSV mezők tabulátorral (`\t`) lesznek elválasztva.

Példa:

**adatok.csv**
```
Nev,Kor,Varos
Anna,25,Budapest
Bela,30,Debrecen
```

**adatok.txt** (kimenet)
```
Nev	Kor	Varos
Anna	25	Budapest
Bela	30	Debrecen
```

## Tesztek

A projekt xUnit modulteszteket tartalmaz (3 teszteset):

```bash
dotnet test Converter.slnx
```

| Teszt | Leírás |
|-------|--------|
| `Convert_ValidCsv_CreatesTabSeparatedTxtFile` | Érvényes CSV → TXT konverzió |
| `Convert_CsvWithQuotedFields_PreservesCommasInsideQuotes` | Idézőjeles mezők kezelése |
| `Convert_MissingFile_ThrowsFileNotFoundException` | Hiányzó fájl hibakezelés |

## Projekt struktúra

```
Converter/
├── Converter/              # Konzol alkalmazás
│   ├── Program.cs
│   └── CsvToTxtConverter.cs
├── Converter.Tests/        # xUnit tesztek
│   └── CsvToTxtConverterTests.cs
├── Converter.slnx
└── README.md
```

## Klónozás után

```bash
git clone <repo-url>
cd Converter
dotnet build Converter.slnx
dotnet test Converter.slnx
dotnet run --project Converter/Converter.csproj -- minta.csv
```

## GitHub-ra feltöltés

Ha még nincs Git telepítve: [git-scm.com/downloads](https://git-scm.com/downloads)

```bash
cd Converter
git init
git add .
git commit -m "CSV to TXT konverter C# konzol alkalmazással és xUnit tesztekkel"
git branch -M main
git remote add origin https://github.com/<felhasznalonev>/Converter.git
git push -u origin main
```

Előtte hozz létre egy üres repository-t a GitHubon (README nélkül, hogy ne legyen merge conflict).
