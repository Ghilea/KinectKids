# KinectKids 3D – Spökjakten

Det här är den nya 3D-grunden för KinectKids. Den ligger bredvid den tidigare WPF-versionen så att den fungerande Kinect-prototypen inte försvinner under ombyggnaden.

## Första spelbara prototypen

- en riktig perspektivkamera i en spökvagn
- kurvande 3D-räls genom fyra sammanhängande miljözoner
- station, blacklight-valv, krypta, monsterverkstad och finalhall
- tredimensionella spöken, zombies och en zombie-konduktör som boss
- mål blir naturligt större när vagnen närmar sig
- båda Kinect-händerna fungerar som separata sikten
- sikta, dra handen tillbaka mot kroppen och kasta den snabbt framåt för att skjuta spökmagi
- kroppshinder där spelaren måste ducka eller väja åt vänster/höger
- ståhöjd och kroppens mitt kalibreras automatiskt per spelare
- en eller två spelare kan samla egna poäng
- musreserv om Kinect inte kan öppnas
- fritt licensierad OGG-musik när `Resources/Audio/RideMusic.ogg` finns, annars procedurgenererad reservmusik
- fuktig sten, mossa, murket trä och rostiga räls som procedurgenererade texturer
- spindelväv, vakande porträtt, kedjor, fladdermöss och flimrande ljus
- separat 32-bitars Kinect-brygga för stabil SDK 1.8-kompatibilitet i 64-bitars Unity

All grafik i den här första versionen byggs av riktiga 3D-objekt när spelet startar. Det gör att vi kan prova kamerafärd, avstånd, tempo och Kinect-sikte innan vi lägger tid på slutliga modeller och animationer.

## Öppna projektet

1. Installera **Unity Hub** och en **Unity 6 LTS**-editor med Windows Build Support.
2. I Unity Hub väljer du **Add project from disk**.
3. Välj mappen `unity\KinectKids3D`.
4. Låt Unity importera projektet. Första gången skapas scenen `Spokjakten3D` automatiskt.
5. Stäng Kinect Explorer, Kinect Studio och den gamla KinectKids-appen.
6. Tryck på **Play** i Unity.

Första gången Play trycks bygger Unity automatiskt `KinectBridge.exe` som x86 och startar den i bakgrunden. Bryggan skickar bara ledpositioner genom en lokal Windows-pipe; ingen nätverksport öppnas och inga bilder sparas eller lämnar datorn. Kinect får upp till 30 sekunder att initieras och statusraden visar vilket startsteg som pågår. Om sensorn inte kan starta växlar prototypen till musläge och visar orsaken uppe till vänster.

## Skapa en version för skolan

Välj **KinectKids → Bygg skolversion för Windows** i Unity. Den färdiga mappen skapas i
`unity\KinectKids3D\Build\Spokjakten3D`. Hela mappen kan kopieras till skolans dator och
startas med `Spokjakten3D.exe`; skolan behöver **inte** installera Unity. Kinect SDK 1.8
och Kinectens vanliga drivrutiner måste däremot finnas på datorn.

## Musik

Vi har valt den 80 sekunder långa loopen **Playground** från
[Free Horror Music Pack - SVL](https://shononoki.itch.io/free-horror-music-pack) av
Shononoki. Paketets sida tillåter kommersiella och icke-kommersiella projekt utan krav
på erkännande. Unity återskapar `Assets\Resources\Audio\RideMusic.ogg` automatiskt och
offline från de mindre källdelarna i `MusicSource`; procedurmusiken finns kvar som reserv.
Licensnoteringen finns i projektets `THIRD_PARTY_NOTICES.md`.

## Kontroller

| Kontroll | Funktion |
|---|---|
| Vänster eller höger Kinect-hand | Sikta på ett spöke |
| Dra tillbaka och kasta handen framåt | Kasta spökmagi |
| Ducka | Undvik den svängande spökbommen |
| Luta kroppen åt visad sida | Väj för anflygande spöken |
| Musen + vänsterklick | Reservsikte och kast utan Kinect |
| `S`/`↓`, `A`/`←`, `D`/`→` | Testa ducka/väj i musläge |
| `Mellanslag` eller `P` | Paus |
| `F11` | Helskärm |
| `R` efter målgång | Ny åktur |

## Nästa produktionssteg

Den här versionen är en **vertical slice/greybox**, inte slutgrafiken. Nästa steg efter att färden och Kinect-siktet har testats på din dator är:

1. modulära 3D-modeller och handgjorda figurer ovanpå det nya texturpasset,
2. animationer och överraskningssekvenser,
3. röster och rumsliga ljudeffekter,
4. fler sorters interaktiva mål och kombopoäng,
5. barnmeny och färdig Windows-build som startas utan Unity.
