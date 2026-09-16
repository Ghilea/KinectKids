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
    *   *Mekanik:* Sikta, dra handen bakåt och gör en snabb kaströrelse framåt för att skjuta spökmagi. Att bara hålla handen på målet ger ingen träff.
    *   *Kroppshinder:* Spelaren måste ducka under bommar och luta kroppen åt angiven sida när faror kommer mot vagnen.
    *   *Miljö:* Procedurgenererad prototypmusik, miljöljud, rörliga fladdermöss, flimrande lampor och överraskningshändelser längs rälsen.
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
*   **Kalibrering:** En enkel vy där barnen ställer sig för att kalibrera sensorn innan varje session.

### 5. NÄSTA STEG
1.  Verifiera Matematikbanan och Simon säger med den fysiska Kinect-sensorn.
2.  Justera rörelsetrösklar efter barnens verkliga avstånd och längd.
3.  Göra en Windows-build av Unity-projektet så Spökjakten 3D kan startas direkt från huvudmenyn.
4.  Därefter bygga Färg & Form Labyrint som nästa pedagogiska modul.

**Notera:** Vi fortsätter använda Kinect SDK v1.8 (.NET 4.8) för att hålla kompatibilitet med skolors äldre utrustning, men vi bygger på en modern .NET Core/Standard arkitektur där möjligt.
