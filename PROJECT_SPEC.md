# Projekt: KinectKids - "Kanon i Realitet" (Skolversion)
## Status: Version 1.2 -> Utveckling av Version 2.0 (Pedagogisk Edition)
## Språk: C# (.NET 6/8), WPF, Microsoft Kinect SDK v1.8

### 1. VISION & MÅL
**Mål:** Att skapa ett plattform för fysisk rörelse i skola där barn "löser" ämnen (matte, språk, logik) via Kinect, och belönas med låsta nöjespel (zombie-tåg, spökjakt).
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
    *   *Mekani:* Kasta magi (handgest) på monster som kommer från tunnelbanan.
5.  **Simon Säger: Rörelselektron**
    *   *Logik:* "Händer upp!", "Ducka!", "Sträck ut!".
    *   *Variation:* 30s, 60s, 90s runder. Topplistor (lokalt utan namnlagring).
6.  **Undvik Hinder: Flyga**
    *   *Logik:* Flyga framåt genom en tunnel där hinder dyker upp plötsligt.
    *   *Kinect:* "Luta kroppen" för att kurva, "Hoppa" (höjd på händer) för att passera under hindret.

### 4. KRAV & KRITERIER FÖR NYA VERSIONER

#### Version 1.3: Pedagogisk Grund (MVP för skolan)
*   **Implementera `IGame`-patern:** Flytta alla specifik logik till nya klasser (`MathGame.cs`, `SimonSaysGame.cs`).
*   **Skapa "ProgressionSystem":** En central databas i minnet (XAML/Settings) som sparar:
    *   `MathLevelUnlocked`
    *   `SpookyAdventureLevelUnlocked`
    *   Total poäng för varje barn.
*   **UI-Överläggning:** Lägg till en meny över spelaren som visar "Nästa uppgift" (t.ex. "Ladda upp: 2+2") när man är i träningsläge.

#### Version 1.4: Nätverk & Multiplayer
*   **Lokal Server (SimpleX/UDP):** Tillåt att flera datorer på skolan kan sända samma "Hinder-Data" till flera Kinect-kameror om de spelar samtidigt på samma station.
*   **Topplistor:** Spärrade topplistor per klassrum som syns i fönstret utan lagring av namn (bara poäng).

#### Tekniska Krav för alla framtida versioner
*   **Säkerhet:** Inga bilder lagras. Endå `SkeletonData` skickas till UI.
*   **USB-kompatibilitet:** Måste fungera på USB 2.0 (baksida av PC). Hantera "Bandbredd-fel" snyggt med svenska meddelanden.
*   **Kalibrering:** En enkel vy där barnen ställer sig för att kalibrera sensorn innan varje session.

### 5. OMDÖMNING AV NÄSTA STEG
Jag behöver hjälp att:
1.  Skapa `MathGame.cs` och `SimonSaysGame.cs` som implementerar `IGame`.
2.  Designa en enkel "ProgressionManager" som spårar vilka spel som är låsta.
3.  Kodar en UI-skärma ("Meny") som visas över spelaren och visar instruktioner (t.ex. "Hitta den blå balongen!").

**Notera:** Vi fortsätter använda Kinect SDK v1.8 (.NET 4.8) för att hålla kompatibilitet med skolors äldre utrustning, men vi bygger på en modern .NET Core/Standard arkitektur där möjligt.
