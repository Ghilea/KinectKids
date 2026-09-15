# KinectKids 3D – Spökjakten

Det här är den nya 3D-grunden för KinectKids. Den ligger bredvid den tidigare WPF-versionen så att den fungerande Kinect-prototypen inte försvinner under ombyggnaden.

## Första spelbara prototypen

- en riktig perspektivkamera i en spökvagn
- kurvande 3D-räls genom fyra sammanhängande miljözoner
- station, blacklight-valv, krypta, monsterverkstad och finalhall
- tredimensionella spöken, zombies och en zombie-konduktör som boss
- mål blir naturligt större när vagnen närmar sig
- båda Kinect-händerna fungerar som separata sikten
- en eller två spelare kan samla egna poäng
- musreserv om Kinect inte kan öppnas
- procedurgenererade träff- och bakgrundsljud

All grafik i den här första versionen byggs av riktiga 3D-objekt när spelet startar. Det gör att vi kan prova kamerafärd, avstånd, tempo och Kinect-sikte innan vi lägger tid på slutliga modeller och animationer.

## Öppna projektet

1. Installera **Unity Hub** och en **Unity 6 LTS**-editor med Windows Build Support.
2. I Unity Hub väljer du **Add project from disk**.
3. Välj mappen `unity\KinectKids3D`.
4. Låt Unity importera projektet. Första gången skapas scenen `Spokjakten3D` automatiskt.
5. Stäng Kinect Explorer, Kinect Studio och den gamla KinectKids-appen.
6. Tryck på **Play** i Unity.

Kinect SDK 1.8 hittas automatiskt från den vanliga installationsmappen. Om sensorn inte kan starta växlar prototypen till musläge och visar orsaken uppe till vänster.

## Kontroller

| Kontroll | Funktion |
|---|---|
| Vänster eller höger Kinect-hand | Sikta |
| Håll siktet på en figur | Fyll mätaren och träffa |
| Musen | Reservsikte utan Kinect |
| `Mellanslag` eller `P` | Paus |
| `F11` | Helskärm |
| `R` efter målgång | Ny åktur |

## Nästa produktionssteg

Den här versionen är en **vertical slice/greybox**, inte slutgrafiken. Nästa steg efter att färden och Kinect-siktet har testats på din dator är:

1. modulära 3D-miljöer och handgjorda figurer,
2. animationer och överraskningssekvenser,
3. riktig musik, röster och rumsliga ljudeffekter,
4. fler sorters interaktiva mål och kombopoäng,
5. barnmeny och färdig Windows-build som startas utan Unity.
