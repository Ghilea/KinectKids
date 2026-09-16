# KinectKids – lär och lek med Kinect 360

En svensk, barnvänlig plattform för **Xbox 360 Kinect (Kinect v1)** på Windows 10. Ett eller två barn löser uppgifter och spelar rörelsespel helt utan handkontroll. Pedagogiska poäng låser upp nöjesspel, samtidigt som inga kamera-, ljud- eller skelettdata sparas.

## Pedagogisk version 1.3 under utveckling

- **Matematikbanan:** träffa ballongen med rätt svar på addition och subtraktion.
- **Simon säger:** händer upp, armar ut, händer tillsammans och ducka.
- anonym lokal progression för Spelare 1 och Spelare 2
- Spökjakten 3D låses upp efter sammanlagt 120 mattepoäng
- gemensamt `IGame`-kontrakt och `GameManager` för nya moduler
- instruktionen för nästa uppgift ligger alltid synlig under träningsspel

## Ny 3D-version under utveckling

En separat Unity-baserad version av Spökjakten finns i [`unity/KinectKids3D`](unity/KinectKids3D). Den första vertical slice-versionen har riktig 3D-räls, spökvagn, flera miljözoner, tredimensionella mål, Kinect-handsikte och en zombie-konduktör som boss. Den finns kvar som belöningsspel och kan startas från huvudmenyn när en Windows-build ligger i `unity/KinectKids3D/Build`.

Se [start- och testinstruktionerna för KinectKids 3D](unity/KinectKids3D/README.md).

## Det som finns i version 1.2

- Ballongjakten med 60-sekundersrundor
- Zombietåget: ett barnvänligt äventyr på räls genom station och tunnel
- tydligt handsikte: håll handen på en zombie tills den gula mätaren fylls
- zombier som kommer närmare och en konduktörsboss mot slutet
- animerad färd med mjuka bakgrundsrörelser och övergång mellan två miljöer
- automatisk spårning av en eller två spelare
- stora, tydliga handmarkörer och barnvänligt gränssnitt
- komplett skelettvy i kalibreringen och under spelet
- Kinect-styrda menyer: håll höger hand över en knapp i drygt en sekund
- kalibreringsvy före varje runda
- individuella poäng i tvåspelarläge
- stabilitetsfilter som motverkar falsk spelare två
- helskärmsläge, paus och omstart
- begripliga svenska fel för saknad ström och otillräcklig USB-bandbredd
- musläge för att prova spelet utan Kinect
- ingen inspelning eller lagring av kamera-, djup- eller skelettdata

## Krav

- Windows 10 (64-bit fungerar; appen byggs som x86 för SDK-kompatibilitet)
- Xbox 360 Kinect med nät-/USB-adapter
- [Kinect for Windows SDK 1.8](https://www.microsoft.com/en-us/download/details.aspx?id=40278)
- .NET Framework 4.8
- Visual Studio 2022 med arbetsbelastningen **.NET desktop development**

Sensorn bör sitta direkt i en USB 2.0-port som har tillräcklig bandbredd. Om appen visar `För lite USB-bandbredd`, stäng andra kameror/USB-ljudenheter och prova en annan portgrupp på datorns baksida.

## Starta på din dator

Enklast är att högerklicka på `scripts/Run.ps1` och välja **Kör med PowerShell**. Skriptet:

1. kontrollerar att Kinect SDK 1.8 finns,
2. hittar Visual Studios MSBuild,
3. bygger Release-versionen,
4. startar spelet.

Om PowerShell blockerar lokala skript kan du öppna PowerShell i projektmappen och köra:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Run.ps1
```

Du kan även öppna `KinectKids.sln` i Visual Studio, välja `Release | x86` och trycka `F5`.

## Kontroller

| Kontroll | Funktion |
|---|---|
| Händerna i Ballongjakten | Smäll ballonger |
| Håll en handring på en zombie | Fyll den gula mätaren och träffa |
| Höger hand över en knapp | Fyll mätaren och tryck på knappen |
| `Mellanslag` | Paus / fortsätt |
| `F11` | Helskärm / fönster |
| `Esc` | Tillbaka / lämna helskärm |

I **Testa med mus** fungerar muspekaren som båda händerna för spelare 1.

## Felsökning

Kör först:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Check-Kinect.ps1
```

Vanliga orsaker:

- **Ingen Kinect hittades:** kontrollera nätadaptern och USB-kabeln.
- **För lite USB-bandbredd:** använd en annan fysisk USB-controller/portgrupp och koppla ur andra högbandbreddsenheter.
- **Microsoft.Kinect.dll saknas vid bygge:** installera Kinect for Windows SDK 1.8, inte SDK 2.0.
- **Blinkande grön lampa:** normalt i vänteläge. När spelet öppnar sensorn ska den bli aktiv.

## Projektstruktur

```text
src/KinectKids/
  Assets/     Stations-, tunnel- och zombieillustrationer
  Game/       Pedagogiska moduler samt Ballong- och Zombietåget-motorer
  Input/      Kinect- och musspårning bakom samma gränssnitt
  Models/     Normaliserad spelardata och mattefrågor
  Services/   Anonym lokal progression
  MainWindow  WPF-gränssnitt och spelläge
unity/        Spökjakten 3D i Unity
scripts/      kontroll, bygge och start
```

## Integritet och säkerhet

Programmet använder bara ledpositionerna som SDK:n beräknar i realtid. Det öppnar inte färgkamerans bildström och skriver ingen sensorinformation till disk eller nätverk. Se till att barnen har fri golvyta och att sensorn står stadigt.

## Licens

MIT. Se [LICENSE](LICENSE).
