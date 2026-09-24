STOPP. Nuvarande scenlayout är fortfarande fel och ska inte justeras vidare. Bygg om chase-vyn från grunden enligt följande krav.

Kameran ska INTE se banan ovanifrån. Det ska INTE finnas någon lång smal ramp som går vertikalt uppåt genom bilden.

Vi ska se scenen ungefär som om en kamera stod längst fram i en verklig slottskorridor och filmade en person som springer MOT kameran.

Perspektivet är absolut viktigast.

Golvet ska vara en bred trapezoid:

smalt långt bak vid flyktpunkten

↓

bredare

↓

MYCKET BRETT längst ner vid kameran

Det innebär att korridorens vänstra och högra vägg visuellt ska konvergera mot en flyktpunkt långt bakom spelaren. Golvet ska fylla nästan hela nederdelen av bilden. Det ska inte se ut som en bro eller ramp.

Placera kamerans horisont ungefär i övre tredjedelen av bilden. Kameran ska vara ungefär i spelarens bröst-/midjehöjd, inte högt ovanför.

Spelaren

Spelaren är framför Greve Gast och springer TOWARD CAMERA. Vi ska därför se spelarens FRAMSIDA.

Standardläget under jakten ska använda en riktig loopad framåtriktad springanimation. Använd INTE sidestep-, look-back-, jump- eller reaktionsposen som standard-run.

Spelaren ska ligga ungefär i mitten horisontellt och omkring 60–70 % ned från bildens överkant. Figuren ska vara betydligt större än Greve Gast eftersom spelaren befinner sig närmare kameran.

Under normal löpning ska huvudet vara vänt ungefär mot kameran/färdriktningen. LookBack används endast tillfälligt som en särskild animation.

Greve Gast

Greve Gast befinner sig LÄNGRE BAK i samma korridor och jagar mot kameran.

Han ska vara mindre än spelaren när han är långt bort. När chase-mätaren ökar ska han gradvis bli större och flyttas visuellt närmare spelaren.

Han får inte kännas fastklistrad ovanpå golvet. Han ska sväva fram genom korridoren och ha riktig chase-animation.

Världen måste röra sig

Spelaren förflyttas inte kontinuerligt framåt genom en stor bana. Istället är det miljön som rör sig MOT kameran.

Golvsegment skapas långt bak vid flyktpunkten, växer visuellt när de närmar sig kameran, passerar under spelaren och försvinner utanför nederkanten. Därefter återanvänds de.

Samma gäller väggsektioner, pelare, rustningar, facklor, banér och annan dekor. De börjar mindre längre bak och passerar ut mot vänster/höger sida när de når kameran.

Det ska därför omedelbart kännas som att korridoren susar förbi spelaren.

Parallax

Bakgrund = nästan stilla.

Avlägsna korridorväggar = långsam rörelse.

Närmare väggar/pelare = snabbare.

Golvet = tydlig rörelse mot nederkanten.

Förgrundsobjekt = mycket snabb rörelse när de passerar kameran.

Det är detta som ska skapa fart.

Ingen statisk korridorbild som hela gameplaymiljön.

En bakgrundsillustration får endast användas längst bak för att skapa djup. Själva korridoren närmast spelaren ska bestå av rörliga/recyclade 2.5D-segment.

Tre lanes

Definiera tre visuella gameplaypositioner:

LEFT | CENTER | RIGHT

Spelaren befinner sig normalt i CENTER och Kinect-sidestep flyttar spelaren till LEFT eller RIGHT.

Banans perspektiv måste vara tillräckligt brett för att tre lanes ska vara tydliga nära spelaren.

Det nuvarande smala mittgolvet är därför fel.

Hinder

Hinder skapas långt bak och rör sig mot kameran tillsammans med världen.

Exempel:

tunna → väj

hål → hoppa

låg bjälke → ducka

svängande yxa → tajmad väjning

Hinder måste ändra storlek med perspektivet. De ska inte teleporteras in som färdigstora sprites.

Första prototypen

Bygg INTE hela spelet ännu.

Gör först EN fungerande korridor på cirka 15 sekunder.

Den måste demonstrera:

riktig RunFront loop för spelaren,

Greve Gast jagande bakom,

tre lanes,

kontinuerligt scrollande golv,

korridorväggar/pelare som passerar kameran,

perspektivskalning,

minst ett hopphinder,

minst ett sidestep-hinder.

Använd placeholders om nödvändigt. Prioriteten är ATT RÖRELSEN OCH PERSPEKTIVET KÄNNS RÄTT.

Lägg inte mer tid på färdig grafik innan detta fungerar.

Kontrollfråga innan implementationen accepteras:

Om man tar bort spelaren och Greve Gast, ska en video av miljön ensam fortfarande tydligt se ut som att kameran färdas snabbt framåt genom en slottskorridor.

Om miljön ser stillastående ut är implementationen fel.

Nästa rum

När korridoren fungerar ska samma runnersystem kunna byta asset-set:

CastleCorridor → Door/Arch Transition → Kitchen

Det ska alltså inte laddas en ny stillbild. Korridormodulerna ersätts gradvis av köksmoduler medan spelaren fortsätter springa.

Exempelvis passerar spelaren genom ett stort valv. Bakom valvet börjar stengolvet få annan design, väggarna förändras, facklor ersätts av kökslampor, tunnor och bord börjar dyka upp och sedan befinner man sig helt i köket.

Ändra inte detta tillbaka till full 3D. Vi använder fortfarande 2.5D. Skillnaden är att 2.5D-assets måste röra sig och perspektivskalas för att skapa illusionen av framåtfärd.
