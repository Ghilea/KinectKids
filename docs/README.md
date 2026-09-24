# KinectKids – gemensam Unity-plattform

KinectKids är en svensk, barnvänlig spelplattform för Xbox 360 Kinect på
Windows. Slutkunden ska bara använda ett program:

```text
%LOCALAPPDATA%/KinectKids/Build/KinectKids/KinectKids.exe
```

Programmet startar i Unitys gemensamma meny. Kinect, input, paus, scenbyten och
återgång till menyn hanteras i samma Unity-process. Unity Hub behövs inte för
att spela den färdigbyggda versionen.

## Starta

Dubbelklicka `STARTA-KINECTKIDS.cmd`. Den startar alltid Unity-versionen och
bygger den automatiskt om programfilen saknas.

Alternativt:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Run.ps1
```

Tvinga ett nytt bygge och starta:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Run.ps1 -Rebuild
```

Bygg utan att starta:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Build.ps1
```

## Nuvarande Unity-spel

- Greve Gast – Style C+ vertical slice med musikstyrd jakt.

Följande äldre spel är ännu inte porterade till Unity och visas därför inte i
slutkundens Unity-meny förrän de har återskapats och verifierats:

- Matematikbanan
- Bokstavsjakten
- Formverkstan
- Mönsterjakten
- Simon säger
- Ballongjakten

De ska återskapas som separata Unity-moduler under
`Assets/KinectKids/Games/`. De ska använda plattformens gemensamma Kinect-input,
ljud, inställningar och pausmeny.

## Projektstruktur

```text
unity/KinectKids3D/Assets/KinectKids/
  Core/       beständiga plattformstjänster
  Menu/       Unitys gemensamma spelmeny och GameRegistry
  Games/      separata Unity-spelmoduler
  SharedArt/  delad grafik
  SharedAudio/delat ljud

src/KinectBridge/  x86-brygga mellan Kinect SDK 1.8 och 64-bitars Unity
```

## Krav för Kinect

- Windows 10/11
- Xbox 360 Kinect med nät-/USB-adapter
- Kinect for Windows SDK 1.8

Kinect SDK körs genom den separata 32-bitarsbryggan. Ingen kamera-, djup- eller
skelettdata sparas till disk eller skickas över nätverket.

## Kontroller

| Kontroll | Funktion |
|---|---|
| Höger hand | Menymarkör |
| Håll över knapp | Välj efter ungefär en sekund |
| W / uppåtpil | Hoppa |
| S / nedåtpil | Ducka |
| A / vänsterpil | Flytta vänster |
| D / högerpil | Flytta höger |
| Shift | Spring |
| Mellanslag | Handling |
| Escape | Gemensam pausmeny |

## Licens

MIT. Se [LICENSE](LICENSE).
