# 2. feladat – CI Pipeline felépítés

## Meglévő kód módosítása -> ✅
- ✅ Generálj tesztreporot (lefedettség, min 60% branch coverage), illetve a terminálon lássa a user a fő lépéseit a programnak.
- ✅ Készíts ar architektúrától a `readme.md`-be mermaid diagram-ot (ai-val is jó, nem kell kézzel)
- ✅ Angol kód, és dokumentáció (`readme.md` is!)
- ✅ A program tudjon `.txt`, `.csv`, és `.json` formátumokba oda-vissza is konvertálni, ha rossz formátumod ad meg dobj exception-t!
	- ✅ Ehhez a Newtonsoft.Json-t használd fel a NuGet library-ből (https://www.nuget.org/packages/newtonsoft.json/)

## Workflow létrehozása -> ❌

- ✅ Hozd létre a projet devops-os vezérlő file-ját: `.github/workflows/ci.yml`
- ❌ Workflow akkor induljon ha valaki`push-olni` vagy
- ❌ Workflow induljon `pull_request` eseményre
- ❌ Ne tudjon senki a main branch-re push-olni. (csak új feature branch -> pull request) 
- ❌ Elavult review-k (pull request-ek) automatikus érvénytelenítése új push esetén

## Build és tesztelés -> ❌

- ❌ A build machine tudja ezekeket a lépéseket mindenképpen (szedd is őket külön hogy a user lássa hol hasalhat el):
- ❌ Forráskód kicsekkolása (`actions/checkout`)
- ❌ .NET SDK telepítése (`actions/setup-dotnet`)
- ❌ Függőségek visszaállítása (`dotnet restore`)
- ❌ Projekt buildelése (`dotnet build`)
- ❌ Tesztek futtatása, report generálás, és
- ❌ Kódlefedettség gyűjtése (`--collect:"XPlat Code Coverage"`)

### Kódlefedettségi report követelményei -> ❌

- ✅ Legalább 60% branch coverage!
- ✅ Report publikálása (`ReportGenerator` vagy `coverlet-reporter`)
- ❌ Report workflow artifactként feltöltése (automatikusan)

## Ezeket próbáld ki hogy mi történik -> ❌

- ❌ Egy `teszt`, `build`, `függőség`, `.csproj-config` szándékos elrontása (minden pipeline-od nézd meg hol romolhat el)
- ❌ Push a távoli repositoryba (push-olj saját, és másik account ról feature branch-be, mi van ha valaki idegen branch-et készít?)
- ✅ Elég beszédes-e a terminal, log, teszt, build pipleine ha valami félre megy, mit javasol a user-nek javításra?
- ✅ Mindenképp feature branch-be (nem main) dolgozz hogy ha balami elb\*szódik akkor könnyű legyen visszaállni.
- ✅ A `git commit -m "<commit message>"` nél a `commit message` mindig beszédes legyen hogy tudd mit tettél bele, de nem kell terjengősen (3-5 szó)

### Pipeline optimalizálása (opcionális) -> ⏳ [TBD]

- NuGet cache beállítása (`actions/cache`)
- `~/.nuget/packages` gyorsítótárazása -> (ha nem változik a package ne töltse/buildelje mindig újra, így gyorsabb lesz a pipeline)
- Ha változnak a függőségek (NuGet, .csproj) vagy dobd e a build-et egy hibaüzenettel, vagy próbáls helyre állítani a megfelelő függőség autopmatikus helyre állításával

## Extra (opcionális) -> ⏳ [TBD]

- Márixos build konfigurálása:
	-  Tesztelés .NET 8 alatt
	-  Tesztelés .NET 9 alatt
	-  Tesztelés .NET 10 alatt
	- Tesztelés deploy-olt állományon (.exe)
	- Eredmények automatikus kiértékelése és összehasonlítása
- `EnricoMi/publish-unit-test-result-action` integrálása
- Sikertelen tesztek megjelenítése Pull Request annotációként


**Megjegyzés:** most látod hogy mennyi infrastruktúrát kell építeni egy viszonylag egyszerű feladathoz, később a kódot el fogjuk bonyolítani, de az infrastruktúrának skálázhatónak kell maradnia, vagyis ha kód bonyolultság nő, az indrastruktúra mögötte ne bonyolódjon vele és fordítva.