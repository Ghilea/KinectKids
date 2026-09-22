Vi ändrar nu den visuella och tekniska riktningen för KinectKids / Spökjakten.

Målet är att bygga spelet huvudsakligen i 2.5D, med låsta/regisserade kameror, i stället för att försöka skapa fulla 3D-miljöer och fulla 3D-karaktärer.

Den viktigaste anledningen är produktionskostnad och konsekvens. Grafiken måste kunna skapas snabbt, återanvändas och hålla samma visuella kvalitet genom hela spelet. Lösningen ska därför prioritera illustrerade 2D-assets, parallaxlager, sprites, enkla animationer och återanvändbara modulära delar.

## Grundprincip

Utgå från:

**"Om kameran aldrig behöver se baksidan av något ska vi inte skapa baksidan."**

Full 3D ska bara användas där det finns ett konkret tekniskt behov. Använd inte full 3D bara för att Unity kan göra det.

Spelet ska visuellt upplevas som en levande animerad sagovärld, trots att stora delar egentligen består av 2D-bilder placerade i lager.

## Visuell stil

Stilen ska vara:

- mörk sagobok
- stiliserad
- tydliga silhuetter
- rena former
- begränsade färgpaletter
- mörkt blå/lila grundljus
- varmt orange/gult från lyktor, facklor och maskiner
- handmålad känsla
- barnvänligt men ibland ganska kusligt
- tidlös snarare än modern fotorealistisk

Undvik:

- fotorealistiska PBR-material
- högdetaljerade 3D-modeller
- avancerad tygfysik
- realistiskt hår
- små detaljer som knappt syns under gameplay
- komplexa modeller som kräver mycket manuell produktion

Det ska vara stilrent och medvetet förenklat.

## Kameror

Kamerorna ska som huvudregel vara låsta eller regisserade.

Spelaren ska inte kunna rotera kameran fritt.

Detta gör att miljö och karaktärer kan byggas specifikt för varje spelvinkel.

Undvik system som förutsätter fri tredjepersonskamera.

## Spelsituation 1: Greve Gast jagar spelaren

Detta är ett chase-spel.

Spelaren springer **mot kameran**.

Spelarens ansikte och framsida är därför synliga.

Greve Gast befinner sig bakom spelaren och jagar denne mot kameran.

Kameran rör sig/regisseras så att spelaren förblir tydligt synlig.

Spelaren använder Kinect för exempelvis:

- springa/rörelse
- väja vänster
- väja höger
- hoppa
- ducka
- eventuellt slå undan eller interagera
- reagera på hinder

Miljön behöver INTE vara full 3D.

Bygg istället miljön av flera 2.5D-lager, exempelvis:

1. spelplan/golv
2. vänster miljölager
3. höger miljölager
4. mellanlager
5. bakgrund
6. förgrundsdetaljer
7. dimma/rök
8. ljuseffekter och partiklar

När spelaren rör sig ska lagren kunna flyttas/skalas olika snabbt för att skapa parallax och illusion av djup.

Hinder kan vara separata sprites eller enkla objekt.

Exempel:

- fallande sten
- låg bjälke
- dörr
- spindelnät
- rustning
- låda
- eld
- hål
- objekt som kommer från sidan

Greve Gast ska primärt byggas som 2.5D-karaktär.

Han behöver inte vara full 3D så länge kameran är låst.

Han ska kunna använda separata kroppslager för enkel puppet-animation, exempelvis:

- hatt
- huvud
- ansikte
- krage
- kropp/kappa
- vänster arm
- höger arm
- vänster hand
- höger hand
- spöksvans/rök

Animationer vi för närvarande räknar med för Greve Gast:

- idle/sväva
- jaga långt bakom
- jaga på mellanavstånd
- jaga nära kameran
- accelerera
- hota
- sträcka sig efter spelaren
- attackera
- skrämma
- arg
- kasta/skapa hinder
- träffad/stunned
- återhämta sig
- lösas upp i dimma
- återvända ur dimma
- seger/hån
- kort sårbar/ledsen pose för storymoment

Designen av Greve Gast är låst i princip:

- hög svart hatt
- smalt och kusligt ansikte
- glödande gula/orange ögon
- spetsig krage
- mörk lila/svart rock
- långa händer/fingrar
- svävande spökkropp istället för vanliga ben
- läskig men fortfarande stiliserad och sagolik

Han ska INTE se ut som en vampyr.

Han är ett spöke.

## Spelarkaraktären

Spelaren ska också kunna byggas som 2.5D eftersom kameran är låst.

I chase-spelet syns spelaren främst framifrån.

Det behövs därför inte full 360-graders karaktärsmodell.

Spelaren ska kunna byggas av separata lager:

- huvud
- hår
- överkropp
- vänster arm
- höger arm
- vänster hand
- höger hand
- höfter
- vänster ben
- höger ben
- skor
- skugga

Första animationsuppsättningen:

- idle
- springa mot kameran
- springa långt bort
- springa nära kameran
- väja vänster
- väja höger
- hoppa
- landa
- ducka
- snubbla
- titta bak mot Greve Gast
- rädd reaktion
- träffad
- resa sig
- lyssna
- interagera
- jubla
- seger

Undvik onödigt komplex gång- eller löpanimation. Tydliga key poses är viktigare än realism.

## Spelsituation 2: Direktör Gnista och hennes maskin

Direktör Gnista är en kvinna.

Den här scenen ska behandlas mer som en animerad illustrerad scen än en 3D-nivå.

Spelaren står på marken en bit framför Gnistas stora maskin.

Kameran är i princip stilla.

Bakgrundsmiljön kan därför vara en stor färdig illustration.

Exempelvis:

- laboratorium
- stora rör
- kablar
- maskin
- väggar
- golv
- hyllor
- detaljer

Det som faktiskt behöver separata lager är främst sådant som ska röra sig:

- Direktör Gnista
- hennes armar/händer
- huvud
- mun/ansikte
- vissa kläddelar
- kugghjul
- mätare
- spakar
- elektriska effekter
- blå energi
- rök/ånga
- blinkande lampor
- eventuella skärmar
- spelaren

Gnista ger instruktioner till spelaren och sjunger.

Gnistas karaktär ska därför prioriteras för:

- tydliga handgester
- ansiktsuttryck
- sång/läppsynk
- peka
- presentera maskinen
- glädje
- oro
- koncentration
- senare mörkare/ondare uttryck

Bakgrunden ska INTE brytas upp i fler objekt än vad animationen kräver.

Om en detalj aldrig ska röra sig kan den vara permanent målad i bakgrundsbilden.

## Spelsituation 3: Spökjakten i vagn

Vagnsektionen kan använda samma 2.5D-princip.

Kameran är fast/regisserad från vagnen.

Räls och bana behöver inte vara full 3D om illusionen kan skapas med lager, scaling och perspektiv.

Miljön kan bestå av:

- bakgrund
- mellanlager
- förgrund
- räls
- väggar
- träd
- slott
- dimma
- spöken
- hinder
- interaktiva mål

Objekt kan skalas och flyttas mot kameran för att skapa känslan av framåtrörelse.

Kurvor kan simuleras genom att bakgrund och lager förskjuts och att räls/vy byts gradvis.

Det behöver inte finnas en fysisk komplett 3D-värld bakom det spelaren ser.

## Asset-strategi

Innan riktiga gameplay-assets skapas ska koncept och stil vara låsta.

Sedan ska assets produceras som faktiska spelresurser och inte som konceptblad.

Riktiga assets ska exporteras med:

- transparent bakgrund där det behövs
- konsekvent skala
- rena kanter
- separata lager där animation kräver det
- tydliga pivotpunkter
- konsekventa namn
- Unity-vänligt format
- helst PNG för 2D-grafik

Presentationsbilder och konceptark ska INTE användas direkt som sprites.

De används endast som designreferens.

## Återanvändning

Allt ska designas för återanvändning.

Exempel på återanvändbara miljödelar:

- stenpelare
- valv
- fönster
- dörrar
- facklor
- lyktor
- tunnor
- lådor
- tavlor
- rustningar
- banér
- spindelnät
- träd
- buskar
- stenar
- dimma
- eld
- gnistor
- spökglöd
- skuggor

Hellre 20 bra återanvändbara delar som kan kombineras på många sätt än 100 unika objekt.

## Animation

Prioritera pose-baserad animation.

Animationerna ska:

- vara tydliga på avstånd
- kunna läsas snabbt under Kinect-gameplay
- ha starka silhuetter
- använda få men tydliga key poses
- kunna återanvändas
- undvika subtil realism

Kameran, UI och gameplay kan köras mjukt även om karaktärernas animation är mer stiliserad och keyframe-baserad.

## Teknisk implementation

Kod och Unity-struktur ska stödja denna nya pipeline.

Vi behöver bland annat system för:

- lagerbaserade 2.5D-scener
- parallax
- sprite sorting
- olika djupnivåer
- character puppet/layer animation
- frame/sprite-animation där det passar bättre
- scaling mot kameran
- scripted camera movement
- scripted scene transitions
- animation events
- Kinect-triggerade animationer
- FX-lager
- återanvändbara scene prefabs
- modulära hinder
- möjlighet att byta ut grafik utan att behöva skriva om gameplaykod

Separation mellan gameplay och visuella assets är viktig.

Vi ska kunna byta en sprite eller bakgrund utan att behöva ändra spelregler.

## Produktionsprincip

Utgå hela tiden från att grafiken huvudsakligen måste kunna produceras med AI-genererade och efterbearbetade 2D-assets.

Undvik därför lösningar som kräver:

- avancerad Blender-modellering
- manuell 3D-sculpting
- komplicerad rigging
- komplex UV-mappning
- hundratals handgjorda animationer

Målet är hög visuell kvalitet genom konsekvent stil, inte teknisk komplexitet.

## Viktig regel vid fortsatt utveckling

Innan du implementerar en lösning som bygger på full 3D, fråga:

**Behöver spelaren faktiskt kunna se detta från flera fria vinklar?**

Om svaret är nej ska 2D/2.5D-lösning prioriteras.

Ändra inte fungerande gameplay i onödan. Anpassa främst renderingen, scenstrukturen och asset-pipelinen till den nya riktningen.

Nuvarande nästa mål är att etablera en första fungerande 2.5D chase-scen där:

- spelaren springer mot kameran
- Greve Gast jagar bakom
- miljön skapar djup genom parallax
- Kinect-rörelser styr väjning, hopp och duckning
- sprites och assets enkelt kan bytas ut senare

Använd placeholder-grafik där slutliga assets ännu inte finns. Arkitekturen ska vara färdig för att riktiga assets successivt ersätter placeholders.
