ÖVERGRIPANDE BESLUT

Vi bygger nu om projektet till en gemensam Unity-baserad KinectKids-plattform.

Unity ska vara den enda runtime-miljön för:

huvudmeny
spelval
Kinect-kalibrering
inställningar
gemensamt UI
gemensamt ljud
gemensam input
alla nuvarande och framtida Kinect-spel

Gamla WPF-program eller andra separata launchers ska alltså på sikt inte användas av slutkunden.

Ta dock inte bort gammal fungerande kod innan motsvarande funktion är porterad, testad och verifierad i Unity.

Målet är ett enda Windows-program, exempelvis:

KinectKids.exe

som startar i en gemensam meny och därifrån laddar respektive spel.

1. Arkitektur

Bygg INTE alla spel i samma scen eller samma kodstruktur.

Unity-projektet ska fungera som en plattform där varje spel är en separat modul.

Föreslagen struktur:

Assets/
├── KinectKids/
│   ├── Core/
│   │   ├── Bootstrap/
│   │   ├── Input/
│   │   ├── Kinect/
│   │   ├── Audio/
│   │   ├── Settings/
│   │   ├── SceneManagement/
│   │   ├── SaveData/
│   │   └── UI/
│   │
│   ├── Menu/
│   │   ├── Scenes/
│   │   ├── Scripts/
│   │   ├── Prefabs/
│   │   └── Art/
│   │
│   ├── Games/
│   │   ├── GreveGast/
│   │   ├── Balloons/
│   │   ├── GhostHunt/
│   │   └── FutureGames/
│   │
│   ├── SharedArt/
│   ├── SharedAudio/
│   └── Debug/

Varje spel ska kunna utvecklas relativt fristående utan att ändringar i ett spel förstör de andra.

2. Bootstrap

Skapa en liten scen:

Bootstrap

Den ska vara första scenen i Build Settings.

Bootstrap startar de globala systemen och går därefter till huvudmenyn.

Globala system ska överleva scenbyten med DontDestroyOnLoad, eller motsvarande välstrukturerad service-lösning.

Exempel:

KinectKidsRoot
├── KinectManager
├── InputManager
├── AudioManager
├── SettingsManager
├── SceneLoader
└── GameSessionManager

Det ska bara finnas en instans av dessa system.

3. Gemensamt Kinect-system

Kinect-integrationen får inte dupliceras inne i varje spel.

Gör ett gemensamt abstraktionslager.

Exempel:

IPlayerInput

med funktioner/data för:

Body position
Left hand
Right hand
Head
Jump
Duck
MoveLeft
MoveRight
Running
PlayerDetected

Kinect ska vara en implementation.

Tangentbord ska vara en annan implementation för debugging.

Exempel:

KinectPlayerInput
KeyboardPlayerInput

Spelen ska prata med IPlayerInput, inte direkt med Kinect SDK.

Då kan alla spel använda samma inputsystem.

Det där är särskilt viktigt. Då slipper vi senare ha fem olika versioner av:

IsJumping()
IsDucking()
LeftHandPosition

utspridda i fem spel.

4. Tangentbordsfallback

Alla spel måste kunna testas utan Kinect.

Standardisera debugkontroller:

W / Up       = Jump
S / Down     = Duck
A / Left     = Move Left
D / Right    = Move Right
Shift        = Run
Space        = Action
Escape       = Pause

Lägg även till möjlighet att simulera två spelare senare.

5. Huvudmenyn

Jag tycker att även huvudmenyn bör använda vår nya Style C+.

Alltså samma:

papper
handritade linjer
kritor/färgpennor
mycket färg
stora tydliga knappar

men inte nödvändigtvis Greve Gast överallt.

Instruktionen:

Skapa en ny huvudscen:

MainMenu

Menyn ska vara barnvänlig och fungera med både:

Kinect
mus
tangentbord

Visuell stil:

Style C+

Det innebär:

handritad skisskänsla
svart blyerts/tusch
ljus pappersbakgrund
färgstarka krit-/pennfärger
enkla former
stora tydliga knappar
lätt animation

Undvik avancerade UI-effekter och realistisk grafik.

6. Kinect-navigation i menyn

Menyn ska på sikt gå att använda helt utan mus.

Implementera en gemensam Kinect-cursor.

Exempel:

höger hand styr markören.

En knapp aktiveras genom:

hand dwell

eller

push gesture

Börja med dwell eftersom det är lättare för barn.

Exempel:

håll handen över knappen i 1 sekund

→ knapp fylls visuellt

→ spelet startar.

Det ska finnas tydlig feedback så barnet förstår vad som händer.

7. Menystruktur

Jag hade börjat ungefär så här:

KINECT KIDS

      SPELA

   [ Greve Gast ]

   [ Ballonger ]

   [ Fler spel... ]

      INSTÄLLNINGAR

      AVSLUTA

När man väljer ett spel:

GREVE GAST

[ SPELA ]

[ SÅ HÄR GÖR DU ]

[ TILLBAKA ]

Agentinstruktion:

Bygg inte spelvalen hårdkodade i MainMenu.

Använd ett GameDefinition ScriptableObject.

Exempel:

GameDefinition
{
    id
    displayName
    description
    thumbnail
    sceneName
    minPlayers
    maxPlayers
    requiresKinect
}

Det betyder att vi senare kan lägga till ett nytt spel genom att skapa:

New GameDefinition

utan att skriva om hela menyn.

8. Game Registry

Skapa:

GameRegistry

som innehåller alla tillgängliga GameDefinition.

MainMenu genererar automatiskt spelkorten från registret.

Det här är mycket bättre än:

if(game == 1)
 LoadScene("Ghost");
else if(game == 2)
 LoadScene("Balloons");

Vi kommer nästan garanterat vilja lägga till fler spel.

9. Gemensam spelstandard

Varje spel bör få samma grundregler.

Alla KinectKids-spel ska stödja ett gemensamt lifecycle:

Load
Prepare
Countdown
Playing
Paused
Finished
ReturnToMenu

Skapa om lämpligt:

IKinectKidsGame

eller en basklass:

KinectKidsGameBase

Varje spel behöver åtminstone:

StartGame()
PauseGame()
ResumeGame()
EndGame()
ReturnToMenu()
10. Gemensam pausmeny

När barnet eller läraren trycker Escape:

PAUS

Fortsätt

Starta om

Till huvudmenyn

Alla spel ska använda samma pausmeny.

Inte en separat för varje spel.

11. Gemensam Kinect-kalibrering

Det här tycker jag vi absolut ska ha.

När programmet startar:

Kinect hittad ✓

Ställ dig framför kameran

        🧍

Spelare hittad ✓

Sedan behöver man inte kalibrera om i varje spel.

Agenten:

Lägg kalibrering och player detection utanför individuella spel.

Ett spel ska bara kunna fråga:

PlayerManager.Player1

och få den redan identifierade spelaren.

Sen kan vi utöka det till:

Player1
Player2
12. Inställningar

En gemensam inställningsmeny bör ha åtminstone:

Musikvolym
Ljudeffekter
Röstvolym

Kinect känslighet

Kinect spegelvändning

Spelområde

Fullskärm

Upplösning

Debug input

Spara det centralt.

Exempel:

settings.json

eller Unitys egen persistent data.

13. Gemensamt ljudsystem

Individuella spel ska inte skapa separata globala AudioManagers.

Använd:

Master
Music
Voice
SFX
UI

via Unity AudioMixer.

Då kan exempelvis Greve Gasts musik ligga i Music, medan hans röst ligger i Voice.

14. Greve Gast-spelet

Sedan ska agenten börja flytta vårt nya spel till:

Assets/KinectKids/Games/GreveGast/

Med:

Scenes/
Scripts/
Art/
Audio/
Timeline/
Animations/
Prefabs/
Effects/

och vår valda slutstil är nu:

STYLE C+

Huvudstil = Style C

med:

högre färgmättnad
mer energi
starkare kritfärger

inspirerat av Style B.

Referensbilden Style C+ är den visuella målbilden.

Håll assets relativt enkla så att samma stil kan reproduceras i stora mängder och enkelt animeras.

15. Gamla Greve Gast 3D

De gamla 3D-miljöerna ska inte längre vara slutlig art direction.

Radera dem dock INTE ännu.

Flytta eller märk gammalt innehåll som exempelvis:

_Legacy/

först när nya system fungerar.

Återanvänd funktionell kod där det är relevant.

16. Det gamla WPF-ballongspelet

Det här skulle jag också explicit skriva:

Det befintliga KinectKids/WPF-ballongspelet ska senare portas till Unity.

Portningen ska INTE innebära att all gammal WPF-kod automatiskt översätts rad för rad.

Återskapa spelets gameplay i Unity med de nya gemensamma systemen:

Kinect input
Player tracking
UI
Audio
Game lifecycle

Behåll originalprojektet som referens tills Unity-versionen är verifierad.

När Unity-versionen motsvarar eller överträffar originalet kan WPF-applikationen arkiveras.

Det är en viktig distinktion. Jag skulle hellre återskapa det ganska enkla ballongspelet ordentligt i Unity än försöka bära över WPF-arkitekturen.

17. Scenstruktur

Jag skulle använda:

00_Bootstrap
01_MainMenu

Games/
GreveGast
Balloons
GhostWhatever

Inte:

Menu + GreveGast + Balloons

allting i en enorm Unity-scen.

SceneLoader sköter växlingarna.

Gärna asynchronous loading:

SceneManager.LoadSceneAsync(...)

så vi senare kan visa en handritad laddningsskärm.

18. Återgång till menyn

Det måste vara mycket robust.

När ett spel avslutas:

stoppa spelljud
↓
släpp spelspecifika resurser
↓
behåll Kinect
↓
behåll globala settings
↓
ladda MainMenu

Kinect får inte startas om varje gång man byter spel.

Det ger onödiga problem med hårdvaran.

19. Delade resurser

Lägg sådant som alla spel kan använda i:

Shared/

Exempel:

handcursor.png
player_detected.png
pause icons
sound buttons
paper backgrounds
arrows
countdown 3-2-1

Men Greve Gast-specifik grafik stannar i Greve Gast-mappen.

20. Assembly Definitions

Jag hade även bett agenten använda assembly definitions när strukturen börjar fungera:

KinectKids.Core
KinectKids.Menu

KinectKids.Games.GreveGast
KinectKids.Games.Balloons

Det hjälper oss undvika att ett spel börjar bero på intern kod från ett annat.

Viktig första milestone

Sedan skulle jag avsluta instruktionen med detta:

IMPLEMENTERA INTE ALLT PÅ EN GÅNG

Börja med Platform Milestone 1.

Gör endast:

inventera nuvarande Unity-projekt
identifiera fungerande Kinect-kod
skapa den nya mappstrukturen
skapa Bootstrap
skapa gemensam InputManager
kapsla befintlig Kinect-funktion bakom gemensamt API
skapa keyboard fallback
skapa MainMenu
skapa GameDefinition
skapa GameRegistry
visa Greve Gast som första spelkort
kunna starta Greve Gast-scenen
kunna återvända till MainMenu

Greve Gast-spelets gameplay ska i denna milestone fungera i stort sett som före ombyggnaden.

Ändra inte hela Greve Gast-banan samtidigt som plattformen byggs om.

Vi separerar:

först plattformsarkitektur
sedan ny grafik/gameplay

Detta minskar risken att flera fungerande system förstörs samtidigt.

När milestone är klar:

kör Unity compilation
fixa alla errors
kontrollera Console
testa med keyboard
testa med Kinect
starta MainMenu
starta Greve Gast
återgå till MainMenu
kontrollera att Kinect fortfarande fungerar

Rapportera därefter:

Created files
Modified files
Moved files
Legacy files
Current architecture
Test results
Known problems
Recommended next milestone

Gör milestone 1 nu. Fortsätt inte automatiskt med nästa stora ombyggnad.

Och ja, jag tycker detta är bättre

Det ger oss i praktiken en Kinect-spelkonsol för PC:

        KINECT KIDS
             |
     ┌───────┼───────┐
     ↓       ↓       ↓
 Greve Gast Ballong  Spel 3
     ↓       ↓       ↓
       gemensam Kinect
       gemensamt ljud
       gemensamma menyer
       gemensamma settings

Det innebär också att framtida idéer blir mycket enklare. Om barnen exempelvis vill ha ett dinosauriespel behöver vi i princip bara göra:

Games/Dinosaurs/

plus ett GameDefinition, så dyker det upp i samma meny.

Jag skulle inte be agenten göra Style C+-ombyggnaden av Greve Gast i samma första uppdrag. Först får vi den gemensamma Unity-plattformen stabil. Därefter tar vi Greve Gast Milestone 1: Style C+ vertical slice och ersätter 3D-innehållet systematiskt. Det blir betydligt säkrare än att byta motorstruktur, UI, grafik och gameplay samtidigt.