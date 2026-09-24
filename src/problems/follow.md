NY RIKTNING FÖR KINECTKIDS / SPÖKJAKTEN
Första spelet: Greve Gast jagar spelaren mot kameran

Vi bygger nu första riktiga versionen av Spökjakten som ett 2.5D chase-spel där spelaren springer mot kameran medan Greve Gast jagar bakifrån.

Det här dokumentet beskriver exakt hur spelet ska byggas, hur scenen ska se ut, vad som är fel med nuvarande försök och vad som ska göras istället.

1. HUVUDIDÉ

Detta är inte ett fritt 3D-spel.

Detta är inte en statisk tavla med en figur ovanpå.

Detta är inte en ramp sedd uppifrån.

Detta är ett 2.5D runner/chase-spel där:

spelaren springer mot kameran
Greve Gast jagar bakifrån i samma korridor
världen rör sig mot kameran
miljön är uppbyggd av lager och återanvändbara moduler
banan består av olika rum/sektioner som följer låten och berättelsen 2. VAD SOM ÄR FEL MED NUVARANDE FÖRSÖK

Nuvarande version ser fel ut eftersom:

kameran känns som att den tittar ovanifrån
mittenbanan ser ut som en upphöjd ramp
miljön känns som en statisk bild
det finns ingen tydlig känsla av att spelaren faktiskt rör sig framåt
scenen visar inte hur världen ska kunna växla mellan korridor, kök, stora salen och andra rum
spelaren ser mer ut som en stillsprite än en riktig spelkaraktär i rörelse
miljön är inte byggd som en spelbar chase-scen

Vi ska inte bygga vidare på det upplägget.

3. DET VI SKA BYGGA ISTÄLLET

Vi ska bygga ett framåtdrivande chase-spel.

Grundprincip

Spelaren är visuellt i stort sett förankrad i gameplayytan medan:

golvet scrollar mot kameran
väggar och miljö passerar
hinder spawnas längre bak och kommer framåt
Greve Gast närmar sig eller faller tillbaka beroende på gameplay
rummen byts över tid

Det ska kännas som att spelaren flyr framåt genom slottet.

4. KAMERA
   Kameran ska vara:
   låst
   regisserad
   låg
   placerad framför spelaren
   riktad bakåt genom korridoren mot flyktpunkten
   Kameran ska INTE vara:
   ovanifrån
   snett uppifrån
   fri roterbar tredjepersonskamera
   centrerad på en upphöjd plattform
   Rätt känsla:

Tänk att kameran står framför spelaren i en slottskorridor och att spelaren springer mot oss, medan Greve Gast kommer bakifrån i samma korridor.

Bildkomposition:
spelaren placeras ungefär nedre mitten
Greve Gast ligger bakom, centrerad längre in i korridoren
korridoren fortsätter djupt bakåt mot en tydlig flyktpunkt
väggar och props ramar in sidorna
förgrundsobjekt får gärna passera nära kameran 5. SPELFLÖDE

Första spelet ska inte vara en enda stillastående korridor.

Det ska vara en runner-bana uppdelad i sektioner.

Exempel på första flödet:
Intro i slottskorridor
Jakt i korridor
Övergång till stora salen
Övergång till köket
Mörk passage / mindre gång
Eventuell övergång till ny storypunkt

Varje sektion har:

eget utseende
egna miljödelar
egna hinder
samma grundkamera
samma gameplayprincip 6. SPELAREN
Spelarens roll

Spelaren springer mot kameran.

Det betyder att vi ser:

ansikte
överkropp
benrörelser
reaktioner
rädsla / fokus / lyssnande
Viktigt

Spelaren får inte kännas som en stillbild.

Vi behöver riktig spelanimation.

Minimikrav på animationer:
idle
run långt bort
run mellanavstånd
run nära kameran
sidestep vänster
sidestep höger
hoppa
landa
ducka
snubbla
träffad
resa sig / recover
titta bakåt
lyssna / reagera
interagera / trycka
jubla / seger
Viktigt om run animation:

Run animation framifrån är en av de viktigaste animationerna i hela spelet.
Den måste kännas som en riktig löpcykel, inte bara en enskild pose.

Ben, armar, kropp och huvud måste röra sig tydligt.

7. GREVE GAST
   Gast ska vara:
   ett spöke
   inte en vampyr
   hotfull
   läskig men sagolik
   tydlig i silhuetten
   lätt att läsa på håll
   Design
   hög svart hatt
   smalt läskigt ansikte
   lysande gula/orange ögon
   spetsig krage
   mörk lila/svart rock
   långa händer/fingrar
   svävande spökkropp
   Gast i chase-spelet

Greve Gast jagar spelaren bakifrån.

Han ska:

synas långt bort i början
gradvis närma sig
kunna attackera / hota / kasta hinder
kunna lösas upp i dimma
kunna återvända ur dimma
kunna reagera när spelaren lyckas / misslyckas
Animationer för Gast:
idle sväv
jaga långt bort
jaga mellanavstånd
jaga nära kameran
snabb rusning
sträcka sig efter spelaren
attackera
hota vänster
hota höger
skrämma / vrål
kasta / skapa hinder
träffad / stun
återhämtning
försvinna i dimma
återvända ur dimma
seger / hån
sårbar / mjukare pose för story 8. MILJÖN

Miljön ska inte vara en tavla.
Den ska vara en spelbar chase-miljö.

Huvudprincip

Miljön byggs av lager och moduler som tillsammans skapar:

fart
djup
variation
tydlig spelbana
Viktigt

Mittenbanan ska inte vara en upphöjd ramp.

Det ska vara en bred, sammanhängande korridor/gång/golv där det är tydligt var spelaren springer.

9. SCENUPPBYGGNAD I LAGER

Scenen ska bestå av separata lager.

Lagerordning bakifrån till framåt:
Lager 1 – Djup bakgrund
korridor långt bort
valv
fönster
månsken
dimma längst bak
flyktpunkt
Lager 2 – Bakre korridor
väggar längre bak
pelare
facklor
statyer
dörröppningar
valv
Lager 3 – Gameplaybana
golvsegment
mittgång
sidoytor
sprickor
variation i stengolv
Lager 4 – Hinderlager
tunnor
hål
svängande yxor
fallande stenar
spindlar
rullande objekt
eld
andra gameplayobjekt
Lager 5 – Spelare
spelarens sprite/animation
centrerad i gameplayzonen
Lager 6 – Greve Gast
chase-animation bakom spelaren
skalar efter avstånd
kan gå mellan olika chase-lägen
Lager 7 – Sidoförgrund
närmare pelare
väggkanter
kedjor
banér
ljus
objekt som passerar kameran
Lager 8 – Förgrundsdimma/effekter
låg dimma
partiklar
rök
ljusglöd 10. HUR RÖRELSEN SKA KÄNNAS

Det viktiga är att det ska se ut som att man kommer framåt.

Detta görs inte genom att flytta kameran i en stor riktig 3D-värld, utan genom att låta världen röra sig mot spelaren/kameran.

Så här ska det fungera:
golvsegment scrollar nedåt mot kameran
sidoväggar rör sig med parallax
pelare, facklor och objekt passerar på sidorna
hinder spawnas långt bak och rör sig framåt
bakgrunden ligger djupast och rör sig minst
förgrundselement rör sig snabbast
Gast skiftar mellan avståndslägen

Det är detta som ska ge känslan av hastighet.

11. RUMSBYTEN

Vi ska inte fastna i bara korridoren.

Låten och spelets berättelse kräver att man går vidare till andra platser.

Första spelet ska därför stödja:
slottskorridor
stora salen
köket
mindre passager
eventuellt trapphus/gångbro
Rumsbyte ska ske genom:
ny bakgrund
nya väggmoduler
nya props
nya hinder
nya ljus/färger inom samma övergripande stil
Viktigt

Kamera och gameplay ska fortfarande kännas lika.
Det är miljön som byts, inte hela spelets struktur.

12. EXEMPEL: KORRIDOR
    Utseende
    stenvalv
    facklor
    rustningar
    banér
    pelare
    dimma
    djup korridor bakåt
    Hinder
    fallande sten
    hål i golvet
    svängande yxa
    rullande tunna
    spindlar
    eld
13. EXEMPEL: KÖK
    Utseende
    stora stenväggar
    spis / ugn
    hyllor
    kopparkärl
    bord
    tunnor
    hängande redskap
    ånga/rök
    Hinder
    glidande tunnor
    låg kastrull / köksredskap att hoppa över
    hängande redskap att ducka under
    eldfläckar
    spökmat / kastade föremål
14. ASSETS – HUR DE SKA TÄNKAS

De assets som redan finns ska användas som grund, men scenen måste byggas så att de används rätt.

Miljöassets
golvsegment
väggsegment vänster/höger
valv
dörröppningar
facklor
rustningar
tunnor
lådor
tavlor
banér
pelare
dimlager
Gast-assets
chase frames
attack frames
dimma/försvinnande
kast/hot
uttryck där det behövs
Spelarassets
run loop
hoppa
ducka
sidestep
träffad
recover
reaktioner
Hinderassets
yxa
tunna
hål
spindel
fallande sten
eld
effekter och varningsikoner 15. TEKNISK PRINCIP
AIN ska tänka så här:

Vi bygger inte en fysisk 3D-nivå.
Vi bygger ett 2.5D scene runner-system.

Systemet ska stödja:
låst kamera
parallax
segmentbaserad bana
återanvändbara miljömoduler
scrollande golv
spawn av hinder
sprite sorting
lagerhantering
rumsbyten
sprite- / puppet-animationer
Gast som chase-fiende
spelare med riktiga animationsstater 16. VAD AIN INTE SKA GÖRA

AIN ska INTE:

bygga stora tomma upphöjda ramper
lägga spelaren mitt på en stillbild
använda sned uppifrån-kamera
göra miljön som en platt bakgrund utan spelbar struktur
anta att en enda korridorbild räcker
göra förgrundsdimma som bara täcker nederdelen av hela skärmen
låsa sig vid symmetriska tavelliknande scener
bygga full 3D-värld när 2.5D räcker
tro att en springpose är samma sak som springanimation 17. VAD AIN SKA GÖRA NU
Nästa konkreta mål

Bygg en första fungerande chase-scen enligt denna struktur:

Måste innehålla:
spelaren springer mot kameran
riktig run animation
Greve Gast jagar bakom
bred slottskorridor
tydligt djup
golv som rör sig mot kameran
väggmoduler som passerar ute på sidorna
minst 2–3 typer av hinder
dimma i flera lager
tydlig känsla av fart
tydlig spelbar bana
Scenen ska visa:
hur gameplayytan fungerar
hur världen känns levande
hur moduler återanvänds
hur chase-spelet faktiskt ska se ut 18. DETTA ÄR DEN KORREKTA MENTALA MODELLEN

AIN ska tänka:

Vi bygger en interaktiv 2.5D runner-bana där spelaren flyr genom slottet mot kameran, medan Greve Gast jagar bakifrån.
Världen scrollar och byter sektioner över tid.
Miljön består av modulära lager och assets som återanvänds.
Målet är fart, tydlighet, djup och variation – inte en stillastående bild.

19. FÖRENKLAD KORTVERSION

Om du behöver en kort sammanfattning:

Spelaren springer mot kameran.
Greve Gast jagar bakifrån.
Kameran är låg och låst, inte ovanifrån.
Golvet scrollar mot kameran.
Väggar, pelare, facklor och props passerar vid sidorna.
Miljön byggs av moduler och lager.
Det ska kännas som att man rör sig framåt.
Banan ska kunna byta från korridor till kök och andra rum.
Spelaren måste ha riktig run animation.
Nuvarande ramp/uppifrån-vy är fel och ska överges.

CameraRoot
låst kamera
låg vinkel
spelaren syns framifrån
ingen fri rotation
RunnerRoot
spelaren hålls inom en relativt fast Z-position
världen rör sig mot kameran
endast sidled/hopp/duck påverkar spelarens gameplayposition tydligt
TrackManager
skapar och återanvänder bansegment
flyttar segment mot kameran
återvinner segment när de passerat kameran
ansvarar för rumstyp: Corridor, Hall, Kitchen osv.
EnvironmentSegment
golv
vänster vägg
höger vägg
bakre dekor
förgrundsdekor
spawnpunkter för hinder
övergångspunkt till nästa segment
ParallaxManager
bakgrund rör sig långsamt
mellanlager snabbare
väggar/golv snabbare
förgrund snabbast
PlayerRunner
Run
DodgeLeft
DodgeRight
Jump
Duck
Hit
Recover
LookBack
Win
animationen måste vara en riktig loop, inte en stillbild
GastChaser
Far
Medium
Near
Attack
ThrowObstacle
Stunned
Disappear
Reappear
avståndet till spelaren styr både sprite-skala och animation
ObstacleSpawner
skapar hinder på vänster/mitt/höger lane
hinder kommer från bakgrunden mot spelaren
exempel: tunna, sten, yxa, hål, eld
senare kökshinder: kastruller, bord, redskap osv.
RoomSequenceController
Corridor
GreatHall
Kitchen
Passage
nästa rum
följer låtens tidslinje
varje rum använder samma runner-system men byter asset-set
TransitionController
låter nästa rum byggas in innan det gamla försvinner
exempel: korridor → valv → kök
inga hårda bildbyten om det inte är avsiktligt
FXLayer
dimma
eld
ljusglöd
partiklar
spökeffekter
ska ligga separat från miljögrafiken
Sorting Layers
Background
FarEnvironment
MidEnvironment
ObstaclesBehind
Gast
Player
ObstaclesFront
Foreground
FX
UI

Den viktigaste implementationen att få rätt först är:

en enda 10–20 sekunders korridorsektion där spelaren springer på riktigt, golvet och väggarna passerar kameran, Greve Gast jagar bakom och två hinder kommer emot spelaren.
Ingen köksmiljö, inga stora storysystem och inga extra effekter innan den sektionen faktiskt känns som att man springer framåt.

inte lägga tid på att göra scenen snygg först. Använd placeholders om det behövs.
