# Greve Gast underlag till utvecklingsagenten

KinectKids Spökjakten • Uppdrag från Dennis • Version 1 • 3 oktober 2026

Bygg en spelbar musikstyrd jakt i det befintliga Unityprojektet. Greve Gast ska komma ur sitt porträtt, jaga spelaren, framföra låten och reagera på barnets rörelser. Hans kropp, ansikte och levande svarta rök ska samverka med låten. Alla tider ska kunna justeras i editorn utan att animationerna görs om.

Börja med en fungerande sekvens från porträttet till första hoppet, glidningen och banbytet. Använd sedan samma system till hela låten. Uppdraget omfattar kod, redigerbara låtdata, verkliga animationsklipp eller fungerande visuella motsvarigheter, prefab, material, effekter och integration i spelscenen.

## Underlag och säkra utgångspunkter

- Greve Gasts Jakt(2).wav är den aktuella ljudmastern. Filen är 372,000 sekunder lång, stereo PCM 16 bit med 48 000 Hz. Använd hela filen och kontrollera längden efter Unityimport.
- bild.png visar korridoren före jakten. bild(1).png visar Greven under jakten och den svarta effekten bakom honom. Båda är visuella referenser till det befintliga spelet.
- Dennis hör första SPRING omkring 21–22 sekunder. 21,50 sekunder är endast en startmarkör att finjustera. Den tidigare uppgiften 19,23 sekunder ska ersättas.
- Övriga tider från den tidigare planeringen är uppskattningar. Låtens ord, senare sektionsgränser och BPM måste kontrolleras mot ljudet. Lås inte hela låten till antagandet 144 BPM.
- Fullständig ordagrann lyric och färdig rigg ingår inte i detta underlag. Hitta befintliga original i projektet. Lägg annars in textutdragen här som arbetsmarkörer och märk obekräftade tider tydligt.

## Resultatet vi vill se

Greven ska kännas som en vuxen manlig spökgreve med egen vilja: självsäker, teatralisk och hotfull, med komiska reaktioner. Spelaren springer mot kameran medan Greven jagar bakom. Barnet ska förstå hopp, glidning och sidbyte genom bild, gest och ljud. Sången är tidsmaster och fortsätter i samma tempo genom hela jakten.

Den senare berättelsen handlar om att rädda Gast, och senare även Direktör Gnista. Gnista är kvinna och ska alltid gestaltas som kvinnlig. Denna leverans bygger jakten och ska inte lägga till en ny berättelse eller ändra karaktärernas roller.

<!-- pagebreak -->

# 1 Läs projektet och koppla in systemet

Läs AGENTS.md och projektets dokumentation. Identifiera Unityversion, render pipeline, Kinectadapter, runner, scenbyggare, ljudstart, befintliga Gastobjekt och animationer. Kontrollera vad som redan fungerar innan du lägger till nya komponenter.

Den nuvarande jakten kan använda sprites i en 3Dmiljö. Behåll den fungerande miljön och kamerariktningen. Anpassa animationslösningen till tillgängliga assets. Ett nytt 3Dsystem eller ett nytt render pipelinebyte är inte ett krav.

## Rekommenderad ansvarsfördelning

Namnen är förslag. Återanvänd befintliga klasser med samma ansvar när det minskar dubbelarbete.

| Del | Ansvar |
| --- | --- |
| GastSongAsset | Ljudreferens, sektioner, cues, offsets och timingstatus som redigerbara data. |
| GastSongClock | Aktuell position i låten och tillstånd för start, paus, fortsättning, sökning och slut. |
| GastCueDirector | Sorterar effektiva eventtider och skickar händelser till animation, rök, miljö och gameplay. |
| GastPerformanceController | Blandar kropp, blick, sångmun, uttryck, närmande och reaktioner. |
| GastSmokeController | Håller röken levande och blandar tillfälliga modifierare. |
| RunnerCueAdapter | Kopplar musikkommandon till befintliga hinder, Kinectinput och bedömning. |
| GastCueEditor | Förhandslyssning, markörer, offsets, waveform och validering. |

Håll alla UnityEditorberoenden i Editor-mapp eller separat editorassembly. Anpassa asmdefs till projektet. Skapa inga dubbla system som samtidigt skriver till samma transform, spelar låten eller bedömer samma hinder.

## Första konkreta leveransen

Integrera en liten fungerande sekvens innan du fyller hela låten med events. Den ska visa PortraitWake, LeanOut, ChaseLaunch, ChaseHover, sånggest, HOPPA, DUCKA som slide och ett banbyte. Greven ska reagera olika på lyckat respektive missat hinder. Sekvensen ska gå att spela med tangentbord när Kinect saknas.

Om scenen skapas genom ett script ska generatorn också uppdateras. Spara inte den enda fungerande integrationen som manuella ändringar i en scen som generatorn sedan skriver över. Alla nya assets och referenser ska tåla en ny scenbyggning.

<!-- pagebreak -->

# 2 Låtens tid och redigerbara cues

Använd en gemensam klocka som följer den spelande ljudfilen. Ett föreslaget grundvärde är AudioSource.timeSamples delat med clip.frequency. Unity anger att timeSamples kan läsas och användas för sökning med mer exakt position än AudioSource.time. Detta ger en gemensam ljudposition; synliga events körs fortfarande på renderade frames.

Om projektet behöver schemalagd start kan AudioSource.PlayScheduled tillsammans med AudioSettings.dspTime användas. Håll en tydlig startstatus så cues inte körs före den schemalagda starten. Följ APIstödet i projektets Unityversion. Räkna inte långa sekvenser med WaitForSeconds eller ackumulerad Time.deltaTime.

## Fält som varje cue behöver

| Fält | Betydelse |
| --- | --- |
| id och sectionId | Stabila unika IDn för cue och sektion. |
| vocalTimeSeconds | Ordet eller musikaccentens position i ljudfilen. Null för en ännu ej tidsatt cue. |
| timingStatus | Untimed, Estimated eller VerifiedByListening. |
| cueOffsetSeconds | Lokal finjustering utan att basmarkören skrivs över. |
| gestureLeadSeconds | Hur långt före ordet Grevens rörelse börjar. |
| warningLeadSeconds | Hur långt före ordet spelaren får visuell varning. |
| impactOffsetSeconds | Hindermötets förskjutning efter ordet. |
| windowBefore och windowAfter | Godkänd rörelsetid före och efter hindermötet. |
| action och animationId | Gest eller acting samt klipp eller annan visuell implementering. |
| gameplayCommand | None, Jump, Slide, LaneLeft, LaneRight eller RunStart. |
| smokePreset och environmentId | Rökaccent och valfri miljöhändelse. |
| reactionPolicy och duration | Resultatreaktion och dess längd. |

Sektionen har egen offset. Låten har global offset. Tider räknas så här, med plus som senare i ljudfilen:

```text
T = vocalTimeSeconds + globalOffset + sectionOffset + cueOffset
GestureStart = T - gestureLeadSeconds
WarningStart = T - warningLeadSeconds
ImpactTime = T + impactOffsetSeconds
WindowOpen = ImpactTime - windowBefore
WindowClose = ImpactTime + windowAfter
ResultTime = WindowClose
```

Ett positivt offset flyttar koreografin senare. Den inspelade rösten flyttas inte. Visa därför både ljudmarkörens originaltid och effektiva händelsetider i editorn. Untimed får aldrig bli 0 sekunder av misstag eller köras i spel.

<!-- pagebreak -->

# 3 Uppspelning sökning och editorverktyg

Bygg spelaren så att varje event avfyras en gång per genomspelning. Under vanlig uppspelning körs förfallna events mellan föregående och aktuell ljudtid, i sorterad ordning. Stabila eventIDn ska skilja exempelvis geststart och resultat från samma cue. Events på exakt starttid ska också hanteras.

Vid kort framehack ska tidslinjen komma ikapp. En gest som börjar sent ska kunna förhandsställas till rätt lokal klipptid. Hinder ska placeras utifrån återstående tid till mötet. Utgångna rörelsefönster ska inte ge barnet flera omedelbara missar efter ett långt stopp. Vid ett större avbrott ska inputbedömningen suspenderas och det nya läget återställas.

## Regler för livscykeln

- Paus ska frysa musik, aktiva fönster, spelrörelse och tidsstyrda modifierare tillsammans. Time.timeScale ensam är ingen tillräcklig musikpaus.
- Fortsättning ska behålla position och engångseventens status. Kameraförlust kan använda samma pausflöde; barnet ska få återgå till kalibrerat läge innan fortsättning.
- Sökning bakåt eller framåt är en uttrycklig operation. Stoppa tillfälliga FX, rensa hinder och återställ sektion, närhet, looping animation och rök från data eller ett checkpointläge.
- Sökning får inte spela alla tidigare krascher, rökexplosioner eller resultat igen. Återställ bestående tillstånd separat från engångseffekter.
- I editorpreview ska ingen poäng eller fångst bokföras. Tillåt ett separat testläge som simulerar success och fail.
- Restart ska rensa runID, eventstatus, poäng, aktiva fönster, animationer, trail och poolade hinder. Slut ska stoppa gameplay och visa den authored slutsekvensen en gång.

## Det Dennis behöver kunna göra utan kod

Visa mm:ss.fff, waveform och cue-lista med filtrering på sektion. Det ska gå att spela, pausa, söka, förhandslyssna på några sekunder runt en markör och skapa en markör vid aktuell ljudposition. Markörer ska kunna dras och även ändras med numeriska fält.

Visa separata punkter för röst, gest, varning och hindermöte. Lägg till Undo, dirty marking och säker sparning av ScriptableObjectdata. En exporterbar JSON eller CSV kan vara ett komplement; definiera ett auktoritativt original så import och Inspectorändringar inte konkurrerar.

Validera saknade klipp, dubbla IDn, tider utanför låten, negativa leadvärden, orimliga fönster, överlappande kommandon och för korta hinderförvarningar. Untimed och Estimated ska synas tydligt. Ändrade offsets kräver att events sorteras om och preview återställs.

<!-- pagebreak -->

# 4 Grevens utseende och animationslager

Behåll projektets etablerade rena, lättanimerade sagostil med mättade färger. Greve Gast är ett manligt spöke, inte en vampyr. Ansiktet ska vara vuxet, listigt och något skrämmande. Han kan le hånfullt och spela oskyldig, men får inte ha ett konstant glatt barnansikte.

Referensbilden från jakten visar önskad karaktär och placering. Den stora svarta ytan bakom honom behöver brytas upp till levande rök med mjuka kanter. Spelaren och hindret i förgrunden måste förbli läsbara.

## Välj en fungerande animationsväg

Använd befintlig rigg om den finns. För en riggad 3Dfigur används lämpliga Animatorlager och masker. För en 2Dfigur i 3Dscenen används uppdelade sprites med leder eller faktiska bildrutor. Gemensamma datakommandon ska kunna styra båda vägarna.

För armgester behövs synliga förändringar i överarm, underarm och hand. För sång behövs en rörlig mun och ansikte. Att bara gunga, skala eller flytta hela originalbilden uppfyller inte animationkravet. Saknade bilddelar ska redovisas och ersättas med en tydligt märkt prototyp tills riktiga assets finns. Dölj inte blockerade animationer bakom tomma klipp.

| Lager | Exempel och blandning |
| --- | --- |
| Grundrörelse | ChaseHover fortsätter under sång och gester. |
| Överkropp | Presentera sig, peka, dirigera, sträcka sig och greppa. |
| Ansikte och mun | Sångmun tillsammans med grin, ilska eller förvåning. |
| Blick | Följ spelaren med korta blickar mot hinder, dörr och porträtt. |
| Position | Banbyte och närmande blandas med grundrörelsen. |
| Sekundärrörelse | Hatt och rock släpar vid acceleration och bromsning. |
| Rök | Simulerar kontinuerligt med tillfälliga accenter. |

Sångmun från amplitud är en prototyp och ska beskrivas som sådan. Analysera amplitud utan nya allokeringar per frame; kör och instrument i hela låten kan annars öppna hans mun fel. Lägg in redigerbara intervall för Grevens röst. Använd manuella munnycklar eller visemer när bättre underlag finns.

Definiera vänster och höger som barnets skärmriktning. När Greven är vänd mot kameran kan skärmvänster kräva hans anatomiska högerarm. Verifiera den synliga gesten med pilar i preview. Namnge kommandon efter spelriktning, inte anatomisk sida.

<!-- pagebreak -->

# 5 Animationerna som agenten ska skapa

Klipp ska ha anticipation, tydlig accent, efterrörelse och recovery. Tider i ett klipp är lokala animationstider. Låtens absoluta tider hör hemma i cues. Underkropp eller svävande bas ska fortsätta när överkroppen arbetar.

| Klipp eller variant | Synligt innehåll | Prioritet |
| --- | --- | --- |
| PortraitWake | Ögon öppnas, huvudet lyfts och litet grin. | P0 |
| PortraitLeanOut | Hand på ram, axlar och kropp passerar ramen. | P0 |
| ChaseLaunch | Kroppen laddar, armar fram, acceleration och bromsning. | P0 |
| ChaseHover | Levande loop med andning och oregelbunden svävning. | P0 |
| SingSelfIntroduce | Hand till bröstet, andra handen ut. | P0 |
| CommandJump | Blick upp, stor uppåtgest och recovery. | P0 |
| CommandSlide | Tydlig nedåtgest med överkroppen framåt. | P0 |
| CommandLaneLeft | Blick och pekning åt skärmvänster. | P0 |
| CommandLaneRight | Blick och pekning åt skärmhöger. | P0 |
| AnnoyedSmall | Kort irriterad min och huvudreaktion vid success. | P0 |
| LaughSmall | Kort skratt med ansikte och axlar vid fail. | P0 |
| SurgeForward | Närmande med tillbakabromsning och coat lag. | P0 |
| TauntComeHere | Retfull vinkning som driver spelaren. | P1 |
| PresentEnvironment | Stor svepgest mot miljön. | P1 |
| ListenAndTurn | Hand vid örat och riktad huvudvridning. | P1 |
| CountOneTwoThree | Tre läsbara räkningsposer och uppladdning. | P1 |
| ReachNearGrab | Hand nära spelaren, fingrar öppnas och stängs. | P1 |
| GrabLeft och GrabRight | Anticipation, räckvidd, miss och recovery. | P1 |
| ChoirReact | Nick eller sidoblick under körsvaret. | P1 |
| DeadpanOops | Stillhet, sidoblick och oskyldig gest. | P1 |
| WarningVase | Blick mot vas, varningshand och kraschreaktion. | P1 |
| FalseSafety | Sökande blick, avslappnade öppna armar, dolt grin. | P1 |
| ScareBoo | Laddning, stor framrusning och övergång till jakt. | P1 |
| FinalReach | Långsam hotfull räckning inför nästan nå dig. | P1 |
| LoseSlip och OutroFade | Förvåning, halkning, protest och rökretreat. | P1 |

P0 behövs för den första spelbara leveransen. P1 byggs när P0 fungerar och ska kunna återanvändas över hela låten. Lägg därefter till ansiktsvarianter, större skratt, grammofonreaktion och finare munformer där de faktiskt behövs.

Som startprofil kan en kommandogest ta 0,6–1,0 sekunder och en resultatreaktion 0,3–0,6 sekunder. Det är designvärden som justeras i preview. Håll rörelseklippen utan konkurrerande root motion när runnern äger världsrörelsen.

<!-- pagebreak -->

# 6 Levande svart rök med djup

Röken ska alltid röra sig runt och under Greven. Skapa en tät svart kärna som följer kroppen, mjukare rökmoln runt kärnan och tunnare trådar i utkanten. Lila eller blå toner får synas sparsamt i kanterna. Nederdelen ska kännas fyllig och ha höjd och djup över golvet.

I bild(1).png bildar den befintliga effekten en stor nästan rektangulär svart yta bakom Greven. Ersätt den synliga platta gränsen med flera överlappande volymer eller partikellager och oregelbundet upplösta kanter. Golvröken ska smälta mot väggarna och inte bära en synlig bildrektangel.

## Skapa effekten för den befintliga render pipelinen

Bygg ett kontinuerligt kärnlager, kroppsmoln i world space, en trail och separata accenter. World space-röken ska kunna dröja kvar när Greven bromsar. Placera flera lager på olika djup och höjder så kameran ser volym. Använd alpha blending för svart rök; en ren additiv effekt ger inte den önskade svarta kärnan.

Om render pipelinen stöder scene depth och soft particles, använd djupbaserad toning nära golv och vägg. Om stöd saknas ska materialet falla tillbaka till mjuka alphakanter och fungerande lager. Förutsätt inte VFX Graph eller HDRP. Anpassa transparenssortering och z-position så rök och sprites inte skär genom varandra.

| Modifierare | Beteende |
| --- | --- |
| IdleFlow | Långsam turbulens, variation i wisps och levande form. |
| MotionTrail | Längre bakåtriktad trail vid acceleration; eftersläp vid bromsning. |
| Threat | Tätare och svartare kärna när Greven kommer nära. |
| MusicAccent | Kort puls från en authored markör; ingen ständig blinkning. |
| SmokeCharge | Röken dras ihop innan launch eller BU. |
| CommandAccent | Uppåtgest ger puff nedåt, sidgest böjer trail motsatt. |
| GrabAccent | Kort ström längs armen under ett greppförsök. |
| FadeOut | Utsläpp minskar; levande partiklar får dö mjukt. |

Blanda modifierare och låt tillfälliga accenter avklinga. Återstarta inte hela partikelsystemet vid varje cue. Ha en gräns för emission, partikelantal och överlappning. Återanvänd burstobjekt och material. Rök nära kamera är dyr om många transparenta ytor täcker bilden.

Lägg täthet, svartnivå, höjd, bredd, falloff, turbulence, trail och accentstyrka i ett redigerbart preset. Gör Normal, Intense och LowCost. Sikta på stabila 60 fps vid 1080p på Dennis dator med GTX 1080 Ti och Ryzen 7 1800X; rapportera vad som faktiskt mättes och ge LowCost om målet inte nås.

<!-- pagebreak -->

# 7 Gameplay som barnet hinner förstå

Koppla cues till befintlig Kinectinput och runner. Samma inputadapter ska stödja tangentbord för utveckling. Visa rätt kommando med enkel symbol och pil, en stor gest från Greven och ljudordet. Debugtext om kamera, tid och intern status ska kunna döljas i vanlig spelvy.

DUCKA motsvarar barnets duckning men spelarens figur gör en slide. Figuren springer mot kameran, kastar sig bakåt med benen mot kameran, glider under hindret och reser sig snabbt. Behåll den etablerade spelaravatarens stil utan hatt. Lägg inte till extra partiklar kring denna rörelse.

## Sambandet mellan ord och hinder

En gest som börjar 0,65 sekunder före ett ord kan vara bra acting, men är inte automatiskt tillräcklig total reaktionstid. Ha separata förvarningar, ett redigerbart hindermöte efter ordet och ett generöst godkänt rörelsefönster. Finjustera mot hur barnen faktiskt spelar.

| Kommando | Första designprofilen att prova |
| --- | --- |
| Jump | Gest före ordet, låg förvarnad barriär, giltigt hopp under fönstret. |
| Slide | Nedåtgest, tydligt överhäng, aktiv duckning eller slide under fönstret. |
| LaneLeft | Pil mot skärmvänster, giltig landning i målbanan. |
| LaneRight | Pil mot skärmhöger, giltig landning i målbanan. |
| RunStart | Startar jakten; bedöm fysisk löpning bara om befintligt inputläge använder det. |

Välj om LaneLeft och LaneRight betyder en bana åt sidan eller en bestämd ytterbana. Första förslag är vänster respektive höger ytterbana, så upprepade kommandon har samma synliga mål. Om befintlig runner använder stegvis byte ska data ange målbana och editorn validera genomförbarheten.

## Illustrativ cue som kan flyttas

Detta är ett systemexempel, inte ett verifierat HOPPA i låten:

```text
VocalTime = 88,00 s       TimingStatus = Estimated
GestureLead = 0,65 s     WarningLead = 1,50 s
ImpactOffset = +0,70 s   WindowBefore = 0,35 s
WindowAfter = 0,35 s     ReactionDuration = 0,45 s
```

Då visas varningen 86,50, gesten börjar 87,35, ordet ligger 88,00, hindret möts 88,70 och resultatet bedöms efter 89,05. Beräkna spawn och rörelse så hindret når spelaren vid ImpactTime även när farten ändras.

En resultathändelse avges en gång. Success ger liten irritation; fail ger kort skratt och ett begränsat visuellt närmande. En nära hand eller ett filmiskt surge får inte orsaka en dold extra collision. Saknad kameraspårning ger paus eller neutral bedömning enligt projektets policy, inte automatiska missar.

<!-- pagebreak -->

# 8 Arbetsmarkörer för introduktionen

Alla tider på denna sida är uppskattade. Lyssna och flytta dem innan de sätts till VerifiedByListening. Första SPRING ska fintrimmas inom den av Dennis angivna regionen 21–22 sekunder. Tabellen anger VocalTime, inte färdig impacttid eller faktisk ordtranskription från ny analys.

| Ungefärlig tid | Ljud eller del | Acting och miljö |
| --- | --- | --- |
| 00:02,5 | Hallå där | PortraitWake, ögon före huvud. |
| 00:05,0 | Vart tror du att du ska | Head tilt, hand mot porträttramen. |
| 00:07–10 | Kör om slottet | Gast lyssnar; subtil rökpuls. |
| 00:11,5 | Nu när du ändå är här | PortraitLeanOut börjar. |
| 00:14,5 | Kan vi väl leka | Mer av kroppen ut, inbjudande hand. |
| 00:16,5 | Ett | Första räkningsposen. |
| 00:18,0 | Två | Andra posen, vikt framåt. |
| 00:19,7 | Tre | Armar bak och laddning. |
| 00:21,5 | SPRING | ChaseLaunch, burst, runner startar. |
| 00:22–25 | Instrumental | Acceleration, overshoot och bromsning. |
| 00:25–28 | Instrumental | ChaseHover och ett lugnt sidbyte. |
| 00:28–31 | Instrumental | Ett filmiskt fake grab utan skadebedömning. |
| 00:31–34 | Inför versen | Tillbaka till sångposition. |

## Relationer kring första SPRING

Skapa launchsekvensen som en återanvändbar cuegrupp. För exempelmarkören 21,50 sekunder börjar kroppens uppladdning omkring 20,70, röken dras ihop omkring 21,05, uttrycket blir shout strax före 21,50 och runnern startar på den effektiva launchmarkören. Låt launch, coat lag, rökburst och bromsning ha separata kanaler.

Greven lämnar porträttet genom att överkroppen och handen passerar ramen. Om scenen använder sprites behöver ramen maskera eller täcka rätt delar under utträdet. Att försvinna från porträttet och plötsligt dyka upp bakom spelaren räcker inte som färdig övergång.

Efter launch ska ChaseHover alltid finnas som bas. Rök följer rörelsen och får fortsätta en stund framåt när Greven bromsar. Denna introduktion är den första visuella kontrollen av att karaktären och röken uppför sig som en sammanhängande figur.

<!-- pagebreak -->

# 9 Arbetsmarkörer för vers och första refräng

Samtliga tider är Estimated. Tidigare förslag får inte flyttas med en enda fast korrigering och sedan märkas verifierade. Lyssna efter varje fras. Exakta tider för refrängens HOPPA, DUCKA, VÄNSTER och HÖGER ska markeras i editorn.

| Ungefärlig tid | Arbetsfras | Grevens kropp och uttryck |
| --- | --- | --- |
| 00:34,0 | Jag är Greve Gast | SingSelfIntroduce, hand till bröstet. |
| 00:36,8 | Jag kommer i en hast | Litet SurgeForward och broms. |
| 00:39,6 | Spring du lilla vännen | TauntComeHere med retfull blick. |
| 00:42,8 | Annars blir du snart min gäst | ReachNearGrab, grin på gäst. |
| 00:45,0 | Körsvaret om hans gäst | ChoirReact. |
| 00:46,5 | Genom salar genom gångar | PresentEnvironment. |
| 00:49,0 | Över golv och under tak | Gest ned och upp som foreshadow. |
| 00:51,8 | Du kan springa allt du orkar | Drar sig självsäkert lite bakåt. |
| 00:54,8 | Men jag följer dina spår | Pekning mot golvet och spelaren. |
| 00:57,0 | Körsvaret | Kort sidoblick. |
| 00:58,3 | Jag hör fötter över golvet | ListenAndTurn; förstärk fotstegen lätt. |
| 01:01,2 | Jag hör dörrar slå igen | Blick mot dörr; cue för dörrsmäll. |
| 01:04,0 | Spring du bara | Två drivande handgester. |
| 01:07,0 | Jag hittar dig igen | SurgeForward med tätare rök. |
| 01:09–11 | Igen igen | Återhämtning och pekning. |
| 01:13,5 | Ett steg | Första räkningsposen. |
| 01:15,8 | Två steg | Kommer närmare, andra posen. |
| 01:18,0 | Tre små steg och sedan | Stor anticipation. |
| 01:20–21 | SPRING | Ny burst, intensivare ChaseHover. |

## Första refrängen

Lägg in Spring spring spring om du kan som större sånggester ovanpå ChaseHover. Vid Greve Gast är efter dig minsann går handen först mot honom själv och sedan mot spelaren.

Kommandona HOPPA, DUCKA, VÄNSTER och HÖGER ska läggas som Untimed tills deras verkliga startpunkter är markerade. Koppla dem till kommandogesterna, förvarningen, ett möte med hindret och en enda resultatreaktion. Inför inga samtidiga motstridiga rörelsekrav.

Fortsätt sångmun och blick under gesterna. Kören ska inte automatiskt starta ett andra hopp eller ett andra duckhinder när den upprepar samma ord. Ange vilka röster som är gameplaykommandon i låtdatan.

<!-- pagebreak -->

# 10 Koreografi genom mitten av låten

Skapa följande sektioner som redigerbara regioner. Sektion 1–3 är introduktion, första vers och första refräng. Sektion 4–9 nedan saknar verifierade gränstider; markera dem i den aktuella ljudfilen. Kör endast tidsatta cues i spel.

## Sektion 4 Instrumental jakt

Låt Greven växla bana oberoende av spelaren, göra kontrollerade grabförsök och glida tillbaka. Han behåller blick på barnet medan axlar, rock och rök följer sidrörelsen. Använd musikaccenter efter lyssning; fyll inte varje beat med ett hinder. Ge plats för att läsa miljön.

## Sektion 5 Köket

Använd befintliga eller nygjorda köksföremål som grytor, hängande redskap, tunna och dörr. Vid KLONG får ett synligt redskap en tydlig rörelse eller träff och Greven reagerar. Vid Hoppsan stannar han till i DeadpanOops. Vid Jag är nästan framme nu kommer han närmare och sträcker sig.

Du är ganska snabb, men vet du vad, jag är snabbare ska få lugn självsäker acting. Spara den stora accelerationen till snabbare. Ett föremål som används som hinder måste få korrekt bana, timing och förvarning. Dekorationer ska inte aktivera dolda collisions.

## Sektion 6 Rörelseföljd

Skapa följden HOPPA, HOPPA, DUCKA, VÄNSTER, HÖGER, HOPPA IGEN som individuella cues med delat reaktionssystem. Bekräfta den verkliga ordningen mot WAV. Om orden ligger för tätt för nya hinder kan vissa vara förstärkning av ett redan aktivt kommando. Editorn ska visa detta uttryckligen.

## Sektion 7 Refräng och vasen

Vid över mattan, genom salen, runt den väldigt stora vasen gestikulerar Greven mot respektive miljödel. Akta vasen får en tydlig varningsgest. KRASCH startar en authored krasch med poolade skärvor och ljud som kompletterar låtens mix. Den var dyr får egen stilla deadpanreaktion. NU BLIR DET JAKT höjer intensiteten med en mjuk övergång.

## Sektion 8 Galleriet

Porträttens ögon följer spelaren; faster Hildegard kan skratta eller röra munnen. Rustningen förvarnas med en liten rörelse före KLONK och kan sedan fungera som hinder. Blinkande ljus och knarrande golv används sparsamt så spelbanan förblir tydlig. Nej de väntar bara på DIG avslutas med en stor pekning mot barnet.

## Sektion 9 Ny rörelsepaus

Bygg DUCKA, HÖGER, VÄNSTER och HOPPA med samma cueformat. Vid För bakom dig kommer jag gör Greven ett kraftigare SurgeForward, men den synliga framrusningen får inte ensam räknas som skada. Skadebedömning ska vara kopplad till ett förvarnat gameplayevent.

<!-- pagebreak -->

# 11 Falsk trygghet källare och final

Sektion 10–17 ska få egna markörer, intensitetsvärden och uttryck. Den slutliga spelbara versionen ska täcka ljudmastern till 06:12, även om slutet innehåller tystnad eller musik utan repliker.

## Sektion 10 Falsk trygghet

Hallå och Var tog du vägen får sökande blick när Greven är längre bort. Nåja då har du väl kommit undan och Jakten är över framförs med lugna öppna armar. Vid Nästan växer grinet. Titta bakom dig får viskande närvaro. BU använder ScareBoo, laddad rök och återinträde. SPRING återupptar intensiv jakt. Byt inte till bakvänd kamera om det förstör den etablerade spelvyn.

## Sektion 11 Källaren

Källargången ska få tätare stämning och mindre miljöljus utan att dölja hinder. Spindeln hänger synligt och Greven tittar upp på Akta. Över tunnan, under repet och runt grepen kan mappas till hopp, slide och sidbyte när timing och bana gör dem möjliga. Vatten, dörrsmäll och körsvaret Greve Gast följs av en självsäker Precispose.

## Sektion 12 Kommandon och svar

Jag säger HOPPA, DUCKA, VÄNSTER och HÖGER får tydliga dirigerande gester. Använd samma skärmriktning som tidigare. Repetitioner från kör eller barn ska förstärka kommandot om inte ett separat hinder uttryckligen är tidsatt. SPRING ska driva energin utan att ändra låtens pitch eller skapa okontrollerad fartökning.

## Sektion 13 Sista uppbyggnaden

Jag kommer närmare och Mycket närmare flyttar Greven i två läsbara steg. Jag kan nästan skapar en paus och nå dig använder FinalReach. Det lågmälda Spring ger ett intensivt uttryck, inte automatiskt en maximal burst. Röken mörknar men döljer inte spelarfiguren.

## Sektion 14 och 15 Slutrefräng och rörelser

Använd de stora sånggesterna och hög intensitet. Stenen och grammofonen ska vara synliga när texten nämner dem; Inte grammofonen får en komisk reaktion. Rörelseföljden är preliminärt HOPPA, HOPPA, DUCKA, VÄNSTER, HÖGER, DUCKA, HÖGER, VÄNSTER och SPRING. Bekräfta varje ord och gör varje krav fysiskt genomförbart.

## Sektion 16 Nedräkning

Fem, fyra, tre, två och ett får varsin laddningspose. Fortare och Jag är här ger närhet och blick. Det sista HOPPA får tydlig förvarning och plats för att landa. Koppla utfallet till ett läsbart finalhinder och en tydlig slutreaktion.

## Sektion 17 Slutet

Va, Nej nej nej och Jag halkade får förvåning, halkning och komisk protest. Du vann den här gången återställer hållningen. Vid hotet om steg bakom dig riktas blicken mot barnet och fotsteg kan höras i miljön. Vi ses snart avslutas med grin och rök som drar sig tillbaka. Resultatet ska fungera även i barnvänligt läge med flera tidigare missar.

<!-- pagebreak -->

# 12 Arbetsordning för agenten

Arbeta i körbara steg. Genomför implementationen och verifiera den; lämna inte uppdraget vid en beskrivning av vad som borde byggas.

## Steg 1 Kartlägg och välj integration

Läs projektet, identifiera ägare till ljud, runner, Kinect, Grevens transform och scenbyggning. Lista tillgängliga assets och välj rigg eller 2Dväg. Kontrollera att den aktuella WAV-filen verkligen används. Dokumentera filnamn som hittas i repo i stället för att anta sökvägar från detta underlag.

## Steg 2 Bygg klocka data och preview

Skapa låtasset, sektioner, cues, tre offsetnivåer, validering och preview. Markera första SPRING mot ljudet. Kontrollera start, paus, restart och sökning innan hela låten fylls med events. Låt editorpreview kunna simulera både success och fail.

## Steg 3 Gör första sekvensen synligt spelbar

Skapa P0animationerna, GreveGastprefab, rökpreset och förvarningar. Integrera porträttutträdet och jakten i befintlig scen eller scenbyggare. Koppla första hopp, slide och sidbyte till riktiga hinder och input. Den första leveransen ska redan innehålla kroppsgester och resultatreaktioner.

## Steg 4 Fyll hela låten med sektioner och cues

Tidsätt resten genom att lyssna. Bygg P1acting, miljöcue-system och genomförbara hindersekvenser för kök, galleri, falsk trygghet, källare och final. Om originalassets saknas ska prototyper märkas tydligt och kunna bytas utan ändrad låtdata.

## Steg 5 Förfina och verifiera

Fintrimma gest, röst och hindermöte separat. Kontrollera skärmvänster och skärmhöger. Profilera svart rök och säkra läsbarhet. Spela hela låten till slut med tangentbord och med Kinect om enheten finns. Kontrollera en ny scenbyggning och en omstart.

## Leverera dessa saker i projektet

- Kod och editorverktyg med serialiserade låtdata och stabila cueIDn.
- GreveGastprefab med fungerande animationsklipp eller motsvarande spriteanimationer, mun, blick och uttryck.
- Rökprefab, alpha-material, presets och lågkostnadsvariant.
- Koppling till runner, hinder, input, kameraförlust och resultat.
- Scen eller uppdaterad scenbyggare med sparade referenser och tillhörande meta-filer.
- Kort README som visar var Dennis ändrar tider, hur preview startas och hur tangentbordstest körs.
- Lista över verifierade cues, uppskattade cues och saknade originalassets. Inga tomma klipp får beskrivas som färdiga animationer.

<!-- pagebreak -->

# 13 Kontroller innan leveransen är klar

| Kontroll | Godkänt resultat |
| --- | --- |
| Ljudmaster | Rätt WAV används; importlängd och cuegränser är kontrollerade. |
| Första SPRING | Markören finjusteras i regionen 21–22 s och launch synkas visuellt. |
| Offsets | Global, sektion och cue kan flyttas var för sig; originaltiden behålls. |
| Framevariation | Ett kort hack tappar inte cues eller skapar dubbla hinder. |
| Paus och sökning | Ingen drift efter paus; sökning ger inga gamla krascher eller poäng. |
| Restart | Samma sekvens går att spela två gånger utan kvarvarande state. |
| Animation | Armar, huvud, blick och mun ändras synligt under fortsatt hover. |
| Skärmriktning | VÄNSTER och HÖGER motsvarar barnet och pilen på skärmen. |
| Gameplay | Förvarning syns; hinder är möjliga; resultat bokförs en gång. |
| Reaktion | Success ger irritation, fail kort skratt och begränsat närmande. |
| Rök | Svart fyllig kärna, mjuka kanter och synligt djup utan svart rektangel. |
| Kinect och tangentbord | Test fungerar utan kamera; kameraförlust ger inga gratis missar. |
| Scenbyggning | Ny genererad scen behåller prefab, ljud, cues och referenser. |
| Hela låten | Alla tidsatta sektioner spelas och slutet avslutas korrekt. |

Testa tidsberäkning och eventordning separat från visuella system. Meningsfulla automatiska kontroller omfattar kombinerade offsets, dubbla IDn, flera events inom en frame, paus, reset och seek utan engångseffekter. Visuella krav måste också granskas i spelet; enhetstester bevisar inte att Greven faktiskt gestikulerar eller att röken har mjuka kanter.

Rapportera vad som testats i Unity och vad som endast kontrollerats i kod. Om Kinect eller Unity inte går att köra ska det framgå. Ange mätt maskin, upplösning, fps eller frametid och vald rökprofil när prestanda redovisas.

## Meddelande att ge agenten

Läs detta underlag och bygg Greve Gasts musikstyrda jakt i vårt befintliga KinectKidsprojekt. Börja med att undersöka projektet och välj en lösning som återanvänder runner, input och scenbyggare. Implementera sedan en fungerande första sekvens med porträttutträde, launch, levande svart rök, sånggester, hopp, slide, sidbyte och success/failreaktioner. Fortsätt med hela låtens sektioner enligt arbetsordningen. Gör verkliga animationer och spelintegration, och gör tiderna redigerbara utan kodändring. Tidskoderna är arbetsmarkörer; lyssna på WAV-filen och märk bara bekräftade cues som verifierade. Första SPRING ligger enligt min lyssning omkring 21–22 sekunder. Redovisa ändrade filer, hur jag provar resultatet, genomförda kontroller och eventuella saknade grafikdelar.

## Tekniska referenser

Unity AudioSource.timeSamples: https://docs.unity3d.com/ScriptReference/AudioSource-timeSamples.html

Unity AudioSource.PlayScheduled: https://docs.unity3d.com/ScriptReference/AudioSource.PlayScheduled.html

Kontrollera motsvarande dokumentation för den Unityversion som projektet faktiskt använder.
