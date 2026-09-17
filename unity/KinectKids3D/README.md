# KinectKids 3D – Spökjakten

Det här är den nya 3D-grunden för KinectKids. Den ligger bredvid den tidigare WPF-versionen så att den fungerande Kinect-prototypen inte försvinner under ombyggnaden.

## Första spelbara prototypen

- en riktig perspektivkamera i en spökvagn
- kurvande 3D-räls genom fyra sammanhängande miljözoner
- slottsport, krypta, kullerstensgård, rustningar, facklor och finalhall
- mer detaljrika tredimensionella spöken, zombies och en zombie-konduktör som boss
- mål blir naturligt större när vagnen närmar sig
- exakt ett sikte per spelare; den mest aktiva handen väljs automatiskt
- håll siktet på målet en kort stund eller gör en lätt knuff framåt för att skjuta spökmagi
- vagnen stannar vid slutbossen, som kastar projektiler tills han är besegrad
- en stor slotts­spindel som firas ner från taket och kräver att spelaren duckar
- gargoyler som bryter ut ur väggarna och kräver att spelaren väjer åt vänster/höger
- animerade pilar visar rörelsen utan att barnet behöver läsa instruktionstext
- kameran sänks och lutar med spelarens duckning eller sidoförflyttning
- sidovålnader kastar sig fram nära vagnen och slumpmässiga spökläten hörs från vänster/höger
- ståhöjd och kroppens mitt kalibreras automatiskt per spelare
- en eller två spelare kan samla egna poäng
- musreserv om Kinect inte kan öppnas
- fritt licensierad OGG-musik när `Resources/Audio/RideMusic.ogg` finns, annars procedurgenererad reservmusik
- handgjorda slotts- och kullerstenstexturer, modellerade murblock med fogar samt procedurgenererat murket trä och rost
- formade lågpolygonmodeller med trasigt tyg och smutsig hud i stället för lysande kapselkroppar
- spindelväv, vakande porträtt, kedjor, fladdermöss och flimrande ljus
- separat 32-bitars Kinect-brygga för stabil SDK 1.8-kompatibilitet i 64-bitars Unity
- cirka tre minuters kurvig slottsåktur med ett kroppsstyrt vägval
- korta quick events, gömda miljömonster, markdimma och en boss som stannar vagnen

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

En egen musikfil kan läggas direkt i `Assets` som `.ogg`, `.mp3`, `.wav`, `.aiff` eller
`.aif`. Unity kopierar den automatiskt till `Resources/Audio/CustomRideMusic` och använder
den före den medföljande musiken. Den följer även med när skolversionen byggs.

## Kontroller

| Kontroll | Funktion |
|---|---|
| Rör den hand du vill använda | Spelet väljer den som enda sikte |
| Håll siktet på målet kort | Kasta spökmagi automatiskt |
| Lätt knuff framåt | Kasta spökmagi direkt |
| Följ de animerade pilarna nedåt | Ducka och låt kameran följa med under spindeln |
| Följ pilarna åt sidan | Väj för gargoylens arm; kameran lutar med kroppen |
| Musen + vänsterklick | Reservsikte och kast utan Kinect |
| `S`/`↓`, `A`/`←`, `D`/`→` | Testa ducka/väj i musläge |
| `Mellanslag` eller `P` | Paus |
| `B` | Hoppa direkt till bossen vid utvecklingstest |
| `F11` | Helskärm |
| `R` efter målgång | Ny åktur |

## Nästa produktionssteg

Den här versionen är en **vertical slice/greybox**, inte slutgrafiken. Nästa steg efter att färden och Kinect-siktet har testats på din dator är:

1. importerade, riggade 3D-modeller och handgjorda figurer ovanpå det nya slotts-/texturpasset,
2. animationer och överraskningssekvenser,
3. röster och rumsliga ljudeffekter,
4. fler sorters interaktiva mål och kombopoäng,
5. barnmeny och färdig Windows-build som startas utan Unity.
