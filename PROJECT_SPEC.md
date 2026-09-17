# Projekt: KinectKids - "Kanon i Realitet" (Skolversion)
## Status: Version 1.3 -> Pedagogisk grund implementerad, integrationstest pågår
## Språk: C# (.NET 6/8), WPF, Microsoft Kinect SDK v1.8

### 1. VISION & MÅL
**Mål:** Att skapa en plattform för fysisk rörelse i skolan där barn "löser" ämnen (matte, språk, logik) via Kinect och belönas med låsta nöjespel (zombie-tåg, spökjakt).
**Nyckelvärd:** Ingen handkontroll. Helt kroppsstyrning. Säker integritet (inga kamerabilder lagras, bara skelettdata i minnet).
**Plattform:** Windows 10/11 x86 (för kompatibilitet med Kinect v1).

### 2. TEKNISK ARKITEKTUR (Backend)
Projekten ska följa principen "Game Agnostic Engine". Alla spel delar samma grunder:

*   **Datamodell (`Models/`):**
    *   `SkeletonData`: Håller koll på alla skelett i realtid.
    *   `GestureRecognizer`: Abstrakt klass för att tolka gesten "Hand Up", "Punch", "Hold".
    *   `PlayerState`: Spelarens status (Poäng, LåsadeSpel[], AktivaHänder).
*   **Spelmotor (`Game/`):**
    *   Alla spel implementerar gränssnittet `IGame`.
    *   Metoden `Update(double dt)`: Uppdaterar fysik, Kinect-logik och "Värld" (hindren/balonger).
    *   Metoden `Render(DrawingContext context)`: Ritar 3D-värden eller 2D-UI ovanpå.
*   **UI/UX (`MainWindow.xaml`):**
    *   En gemensam meny som växlar mellan "Skolmode" (fokus på uppgift) och "Nöjesmode" (fokus på action).
    *   Menykrav: Håll höger hand stilla > 2s för att öppna.

### 3. SPELSPELARE LISTA (Prioritering)
Vi ska bygga dessa spel i ordning baserat på komplexitet och pedagogisk värde:

#### Kategori A: Träningsmoduler (Pedagogik) - Låsta först, låses upp vid framgång
1.  **Mattematiken i Banan (Railshooter Lite)**
    *   *Logik:* En bana med texturer som dyker upp. Texten är tal eller symboler ("2+2", "Längre").
    *   *Kinect:* Barnet måste peka på rätt svar eller hålla händerna i ett "Plus-tecken"-format för att fånga balongerna med rätta svar.
    *   *Pedagogik:* Addition/Subtraktion, Mönsterkänsla.
2.  **Färg & Form Labyrint**
    *   *Logik:* Hindren är geometriska former i fel färger. Balongerna har rätt form/färg.
    *   *Kinect:* "Sortera" genom att hålla händerna bredvid varandra för rött, skilda för blått.
3.  **Ordboken (Språk)**
    *   *Logik:* Ord dyker upp i bakgrunden ("BIL", "BALLON").
    *   *Kinect:* Barnet säger ordet högt (mikrofon-integration via Kinect API) eller pekar med handen för att stämma av.

#### Kategori B: Nöjesmoduler (Belöning) - Låsta tills poängmål nås i A
4.  **Spökjakt: Slottet** (Ny utveckling från "Zombietåg")
    *   *Logik:* En renad version av Zombietåget med "spöken" istället för zombies, mörkare tema.
    *   *Mekanik:* Varje spelare har exakt ett sikte. Spelet väljer automatiskt den mest aktiva handen och fungerar därför för både vänster- och högerhänta. En träff avfyras efter en kort mållåsning eller en lätt framåtknuff.
    *   *Kroppshinder:* En stor spindel firas ner från slottstaket och kräver duckning. Gargoyler och stenklor slår ut ur väggarna och kräver att spelaren lutar sig åt angiven sida. Slutbossen kastar dessutom projektiler som kräver samma rörelser.
    *   *Bossfas:* Vagnen stannar i finalhallen och kör inte vidare förrän bossens livsmätare är tom.
    *   *Miljö:* Fritt licensierad musik med lokal reservmusik, ett mörkt slott där facklor lokalt avslöjar modellerade murblock, sten- och kullerstenstexturer, rustningar, krypta, spindelväv, rörliga fladdermöss och överraskningshändelser längs rälsen.
    *   *Figurer:* Sammanhängande lågpolygonmodeller med trasiga silhuetter och egna smuts-, tyg- och hudmaterial används i prototypen. Enbart ögon och små magiska detaljer får självlysning.
    *   *Tillgänglighet:* Kroppens mitt och ståhöjd kalibreras separat för varje spårad spelare. Mus och tangentbord finns kvar som testläge.
5.  **Simon Säger: Rörelselektron**
    *   *Logik:* "Händer upp!", "Ducka!", "Sträck ut!".
    *   *Variation:* 30s, 60s, 90s runder. Topplistor (lokalt utan namnlagring).
6.  **Undvik Hinder: Flyga**
    *   *Logik:* Flyga framåt genom en tunnel där hinder dyker upp plötsligt.
    *   *Kinect:* "Luta kroppen" för att kurva, "Hoppa" (höjd på händer) för att passera under hindret.

### 4. KRAV & KRITERIER FÖR NYA VERSIONER

#### Version 1.3: Pedagogisk Grund (MVP för skolan)
*   **Implementera `IGame`-mönstret:** `MathGame.cs` och `SimonSaysGame.cs` finns bakom ett gemensamt kontrakt och en `GameManager`.
*   **Skapa "ProgressionSystem":** En lokal anonym XML-fil sparar:
    *   `MathLevelUnlocked`
    *   `SpookyAdventureLevelUnlocked`
    *   Total poäng för varje barn.
*   **UI-överläggning:** Nästa uppgift eller rörelse visas över spelaren under hela träningsrundan.
*   **Belöningskoppling:** Spökjakten 3D ligger kvar i Unity-projektet och låses upp efter 120 mattepoäng.

#### Version 1.4: Nätverk & Multiplayer
*   **Lokal Server (SimpleX/UDP):** Tillåt att flera datorer på skolan kan sända samma "Hinder-Data" till flera Kinect-kameror om de spelar samtidigt på samma station.
*   **Topplistor:** Spärrade topplistor per klassrum som syns i fönstret utan lagring av namn (bara poäng).

#### Tekniska Krav för alla framtida versioner
*   **Säkerhet:** Inga bilder lagras. Endå `SkeletonData` skickas till UI.
*   **USB-kompatibilitet:** Måste fungera på USB 2.0 (baksida av PC). Hantera "Bandbredd-fel" snyggt med svenska meddelanden.
*   **Unity/Kinect-kompatibilitet:** Unity kör som x64 och tar emot leddata från `KinectBridge.exe`, som kör Kinect SDK 1.8 som x86 genom en lokal Windows-pipe. Ingen nätverksport öppnas och ingen sensorbild skickas eller lagras.
*   **Skoldistribution:** Skolor får en färdig Windows-mapp med EXE, datafiler och Kinect-brygga. Unity Hub eller Unity Editor får inte krävas på skoldatorn.
*   **Kalibrering:** En enkel vy där barnen ställer sig för att kalibrera sensorn innan varje session.

### 5. NÄSTA STEG
1.  Verifiera Matematikbanan och Simon säger med den fysiska Kinect-sensorn.
2.  Justera rörelsetrösklar efter barnens verkliga avstånd och längd.
3.  Göra en Windows-build av Unity-projektet så Spökjakten 3D kan startas direkt från huvudmenyn.
4.  Därefter bygga Färg & Form Labyrint som nästa pedagogiska modul.

**Notera:** Vi fortsätter använda Kinect SDK v1.8 (.NET 4.8) för att hålla kompatibilitet med skolors äldre utrustning, men vi bygger på en modern .NET Core/Standard arkitektur där möjligt.
